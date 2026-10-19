using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Shared.UtilityServices;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;

public partial class AssessmentManufacturingCategoryEditor : ComponentBase
{
    [Inject] private IGenericDataRepository<AssessmentManufacturingMaterial> MaterialRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentManufacturingBalances> BalancesRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentSalesCategory> CategoryRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<Assessment> AssessmentRepository { get; set; } = default!;
    [Inject] private IProjectionStateManager ProjectionStateManager { get; set; } = default!;
    [Inject] private ZabOffCanvasService OffCanvasService { get; set; } = default!;
    [Inject] private ISessionService SessionService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private ILogger<AssessmentManufacturingCategoryEditor> Logger { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }
    [Parameter] public long AssessmentSalesCategoryId { get; set; }
    [Parameter] public ManufacturingInventoryAspect Aspect { get; set; }

    private string CategoryName { get; set; } = string.Empty;
    private List<AssessmentManufacturingMaterial> Materials { get; set; } = [];
    private AssessmentManufacturingBalances Balances { get; set; } = new();
    private MaterialEditor? CurrentMaterial { get; set; }
    private bool IsLoading { get; set; }
    private bool IsSaving { get; set; }
    private bool IsLegacyBalances { get; set; }
    private decimal DefaultVatRate { get; set; }
    private decimal[] ClosingValues { get; set; } = new decimal[12];

    private string CurrencyCode => SessionService.CurrencyCode ?? "ZAR";
    private string AspectLabel => Aspect switch
    {
        ManufacturingInventoryAspect.WorkInProgress => "Work in Progress (WIP)",
        ManufacturingInventoryAspect.FinishedProducts => "Finished Products",
        _ => "Raw Materials"
    };

    private decimal OpeningValue
    {
        get => Aspect == ManufacturingInventoryAspect.WorkInProgress
            ? Balances.OpeningWorkInProgress
            : Balances.OpeningFinishedGoods;
        set
        {
            if (Aspect == ManufacturingInventoryAspect.WorkInProgress)
                Balances.OpeningWorkInProgress = value;
            else
                Balances.OpeningFinishedGoods = value;
        }
    }

    private int UnassignedMaterialCount => Materials.Count(x =>
        !x.AssessmentSalesCategoryId.HasValue);

    protected override async Task OnParametersSetAsync()
    {
        if (AssessmentId <= 0 || AssessmentSalesCategoryId <= 0)
            throw new InvalidOperationException("An assessment and sales category are required.");

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            var categoryTask = CategoryRepository.GetByIdAsync(AssessmentSalesCategoryId);
            var assessmentTask = AssessmentRepository.GetByIdAsync(AssessmentId);
            await Task.WhenAll(categoryTask, assessmentTask);

            var category = await categoryTask;
            if (category is null
                || category.AssessmentId != AssessmentId
                || !category.Active)
            {
                throw new InvalidOperationException(
                    "The selected sales category is inactive or does not belong to this assessment.");
            }

            CategoryName = category.AssessmentSalesCategoryName ?? "Sales category";
            DefaultVatRate = (await assessmentTask)?.VATRate ?? 0m;

            if (Aspect == ManufacturingInventoryAspect.RawMaterials)
            {
                var materials = await MaterialRepository.GetAllAsync(x =>
                    x.AssessmentId == AssessmentId
                    && x.Active
                    && (x.AssessmentSalesCategoryId == AssessmentSalesCategoryId
                        || x.AssessmentSalesCategoryId == null));
                Materials = materials
                    .OrderBy(x => x.AssessmentSalesCategoryId.HasValue ? 0 : 1)
                    .ThenBy(x => x.Description)
                    .ToList();
                return;
            }

            var categoryBalancesTask = BalancesRepository.GetAllAsync(x =>
                x.AssessmentId == AssessmentId
                && x.Active
                && x.AssessmentSalesCategoryId == AssessmentSalesCategoryId);
            var legacyBalancesTask = BalancesRepository.GetAllAsync(x =>
                x.AssessmentId == AssessmentId
                && x.Active
                && x.AssessmentSalesCategoryId == null);
            await Task.WhenAll(categoryBalancesTask, legacyBalancesTask);

            var categoryBalances = (await categoryBalancesTask).FirstOrDefault();
            var legacyBalances = (await legacyBalancesTask).FirstOrDefault();
            IsLegacyBalances = categoryBalances is null && legacyBalances is not null;
            Balances = categoryBalances
                ?? legacyBalances
                ?? new AssessmentManufacturingBalances
                {
                    AssessmentId = AssessmentId,
                    AssessmentSalesCategoryId = AssessmentSalesCategoryId
                };

            var rawClosingValues = Aspect == ManufacturingInventoryAspect.WorkInProgress
                ? Balances.MonthlyClosingWorkInProgress
                : Balances.MonthlyClosingFinishedGoods;

            ClosingValues = rawClosingValues.Select(v => Math.Round(v, 2)).ToArray();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception,
                "Could not load manufacturing {Aspect} for category {CategoryId} in assessment {AssessmentId}",
                Aspect,
                AssessmentSalesCategoryId,
                AssessmentId);
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CreateMaterial() =>
        CurrentMaterial = new MaterialEditor(new AssessmentManufacturingMaterial
        {
            AssessmentId = AssessmentId,
            AssessmentSalesCategoryId = AssessmentSalesCategoryId,
            VATRate = DefaultVatRate,
            Active = true
        });

    private void EditMaterial(AssessmentManufacturingMaterial material) =>
        CurrentMaterial = new MaterialEditor(material);

    private void CancelMaterialEdit() => CurrentMaterial = null;

    private async Task SaveMaterialAsync()
    {
        if (CurrentMaterial is null || IsSaving)
            return;

        var editor = CurrentMaterial;
        if (string.IsNullOrWhiteSpace(editor.Data.Description)
            || string.IsNullOrWhiteSpace(editor.Data.UnitOfMeasure))
        {
            Toast.ShowError("Enter a material name and unit of measure.", SessionService.AppTitle);
            return;
        }

        if (editor.Data.OpeningQuantity < 0m
            || editor.Data.OpeningValue < 0m
            || editor.MonthlyPurchaseQuantities.Any(x => x < 0m)
            || editor.MonthlyPurchaseValues.Any(x => x < 0m)
            || editor.MonthlyClosingQuantities.Any(x => x < 0m))
        {
            Toast.ShowError(
                "Material quantities and values must be non-negative.",
                SessionService.AppTitle);
            return;
        }

        try
        {
            IsSaving = true;
            editor.Data.AssessmentId = AssessmentId;
            editor.Data.AssessmentSalesCategoryId = AssessmentSalesCategoryId;
            editor.Data.VATRate = editor.Data.IncludeVAT ? DefaultVatRate : 0m;
            editor.Data.OpeningQuantity = Math.Round(editor.Data.OpeningQuantity, 2);
            editor.Data.OpeningValue = Math.Round(editor.Data.OpeningValue, 2);
            editor.Data.MonthlyPurchaseQuantities = editor.MonthlyPurchaseQuantities.Select(v => Math.Round(v, 2)).ToArray();
            editor.Data.MonthlyPurchaseValues = editor.MonthlyPurchaseValues.Select(v => Math.Round(v, 2)).ToArray();
            editor.Data.MonthlyClosingQuantities = editor.MonthlyClosingQuantities.Select(v => Math.Round(v, 2)).ToArray();
            editor.Data.TotalNoVAT = Math.Round(editor.Data.MonthlyPurchaseValues.Sum(), 2);
            editor.Data.TotalWithVAT = editor.Data.IncludeVAT
                ? Math.Round(editor.Data.TotalNoVAT * (1m + editor.Data.VATRate / 100m), 2)
                : editor.Data.TotalNoVAT;

            await MaterialRepository.SaveAsync(editor.Data);
            Materials = (await MaterialRepository.GetAllAsync(x =>
                    x.AssessmentId == AssessmentId
                    && x.Active
                    && (x.AssessmentSalesCategoryId == AssessmentSalesCategoryId
                        || x.AssessmentSalesCategoryId == null)))
                .OrderBy(x => x.AssessmentSalesCategoryId.HasValue ? 0 : 1)
                .ThenBy(x => x.Description)
                .ToList();
            await ProjectionStateManager.InvalidateDataAsync(
                "manufacturing-materials",
                AssessmentId,
                AssessmentId);
            await OffCanvasService.PublishResultAsync(
                SaveResult.SavedAndContinue("Raw-material estimates saved."));
        }
        catch (Exception exception)
        {
            Logger.LogError(exception,
                "Could not save raw material for category {CategoryId} in assessment {AssessmentId}",
                AssessmentSalesCategoryId,
                AssessmentId);
            await OffCanvasService.PublishResultAsync(
                SaveResult.Failed($"Could not save raw-material estimates: {exception.Message}"));
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task DeactivateMaterialAsync(AssessmentManufacturingMaterial material)
    {
        try
        {
            material.Active = false;
            material.ModifiedDate = DateTime.UtcNow;
            await MaterialRepository.SaveAsync(material);
            await ProjectionStateManager.InvalidateDataAsync(
                "manufacturing-materials",
                AssessmentId,
                AssessmentId);
            await OffCanvasService.PublishResultAsync(
                SaveResult.SavedAndClose("Raw material deactivated."));
        }
        catch (Exception exception)
        {
            Logger.LogError(exception,
                "Could not deactivate raw material {MaterialId} in assessment {AssessmentId}",
                material.AssessmentManufacturingMaterialId,
                AssessmentId);
            await OffCanvasService.PublishResultAsync(
                SaveResult.Failed($"Could not deactivate raw material: {exception.Message}"));
        }
    }

    private async Task SaveBalancesAsync()
    {
        if (IsSaving)
            return;
        if (OpeningValue < 0m || ClosingValues.Any(x => x < 0m))
        {
            Toast.ShowError("Inventory balances cannot be negative.", SessionService.AppTitle);
            return;
        }

        try
        {
            IsSaving = true;
            Balances.AssessmentId = AssessmentId;
            Balances.AssessmentSalesCategoryId = AssessmentSalesCategoryId;
            var roundedClosingValues = ClosingValues.Select(v => Math.Round(v, 2)).ToArray();

            if (Aspect == ManufacturingInventoryAspect.WorkInProgress)
                Balances.MonthlyClosingWorkInProgress = roundedClosingValues;
            else
                Balances.MonthlyClosingFinishedGoods = roundedClosingValues;

            await BalancesRepository.SaveAsync(Balances);
            await ProjectionStateManager.InvalidateDataAsync(
                "manufacturing-balances",
                AssessmentId,
                AssessmentId);

            await OffCanvasService.PublishResultAsync(
                SaveResult.SavedAndClose($"{AspectLabel} balances saved."));

        }
        catch (Exception exception)
        {
            Logger.LogError(exception,
                "Could not save manufacturing {Aspect} for category {CategoryId} in assessment {AssessmentId}",
                Aspect,
                AssessmentSalesCategoryId,
                AssessmentId);
            await OffCanvasService.PublishResultAsync(
                SaveResult.Failed($"Could not save {AspectLabel} balances: {exception.Message}"));
        }
        finally
        {
            IsSaving = false;
        }
    }

    private string Money(decimal amount) =>
        DisplayUtility.FormatCurrency(amount, CurrencyCode, SessionService.CultureName);

    private sealed class MaterialEditor
    {
        public MaterialEditor(AssessmentManufacturingMaterial data)
        {
            Data = data;
            MonthlyPurchaseQuantities = data.MonthlyPurchaseQuantities.Select(v => Math.Round(v, 2)).ToArray();
            MonthlyPurchaseValues = data.MonthlyPurchaseValues.Select(v => Math.Round(v, 2)).ToArray();
            MonthlyClosingQuantities = data.MonthlyClosingQuantities.Select(v => Math.Round(v, 2)).ToArray();
        }

        public AssessmentManufacturingMaterial Data { get; }
        public bool IsNew => Data.AssessmentManufacturingMaterialId == 0;
        public decimal[] MonthlyPurchaseQuantities { get; set; }
        public decimal[] MonthlyPurchaseValues { get; set; }
        public decimal[] MonthlyClosingQuantities { get; set; }
    }
}