using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.FinancialCalculations;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Shared.UtilityServices;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;

public partial class AssessmentManufacturingInputComponent : ComponentBase
{
    [Inject] private IGenericDataRepository<AssessmentSalesCategory> CategoryRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentManufacturingMaterial> MaterialRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentManufacturingBalances> BalancesRepository { get; set; } = default!;
    [Inject] private ZabOffCanvasService OffCanvasService { get; set; } = default!;
    [Inject] private ISessionService SessionService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private ILogger<AssessmentManufacturingInputComponent> Logger { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }

    private List<AssessmentSalesCategory> Categories { get; set; } = [];
    private List<AssessmentManufacturingMaterial> Materials { get; set; } = [];
    private List<AssessmentManufacturingBalances> Balances { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private int UnassignedMaterialCount => Materials.Count(material =>
        !material.AssessmentSalesCategoryId.HasValue
        || Categories.All(category =>
            category.AssessmentSalesCategoryId != material.AssessmentSalesCategoryId.Value));
    private bool HasUnassignedBalances => Balances.Any(balance =>
        !balance.AssessmentSalesCategoryId.HasValue
        || Categories.All(category =>
            category.AssessmentSalesCategoryId != balance.AssessmentSalesCategoryId.Value));

    private string CurrencyCode => SessionService.CurrencyCode ?? "ZAR";

    protected override async Task OnParametersSetAsync()
    {
        if (AssessmentId <= 0)
            throw new InvalidOperationException("An assessment is required for Manufacturing inventory.");

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            var categoriesTask = CategoryRepository.GetAllAsync(x =>
                x.AssessmentId == AssessmentId && x.Active);
            var materialsTask = MaterialRepository.GetAllAsync(x =>
                x.AssessmentId == AssessmentId && x.Active);
            var balancesTask = BalancesRepository.GetAllAsync(x =>
                x.AssessmentId == AssessmentId && x.Active);
            await Task.WhenAll(categoriesTask, materialsTask, balancesTask);

            Categories = (await categoriesTask)
                .OrderBy(x => x.AssessmentSalesCategoryName)
                .ToList();
            Materials = (await materialsTask).ToList();
            Balances = (await balancesTask).ToList();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception,
                "Could not load manufacturing inventory for assessment {AssessmentId}",
                AssessmentId);
            Toast.ShowError($"Could not load manufacturing inventory: {exception.Message}", SessionService.AppTitle);
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private CategoryInventory GetInventory(long categoryId)
    {
        var rawMaterials = new decimal[12];
        foreach (var material in Materials.Where(x => x.AssessmentSalesCategoryId == categoryId))
        {
            var closingValues = ManufacturingProjectionCalculator.CalculateMonthlyClosingValues(material);
            for (var month = 0; month < 12; month++)
                rawMaterials[month] += closingValues[month];
        }

        var balances = Balances.FirstOrDefault(x => x.AssessmentSalesCategoryId == categoryId);
        var wip = balances?.MonthlyClosingWorkInProgress ?? new decimal[12];
        var finished = balances?.MonthlyClosingFinishedGoods ?? new decimal[12];
        var total = Enumerable.Range(0, 12)
            .Select(month => rawMaterials[month] + wip[month] + finished[month])
            .ToArray();
        return new CategoryInventory(rawMaterials, wip, finished, total);
    }

    private decimal GetGrandTotal(int month) => Categories.Sum(category =>
        GetInventory(category.AssessmentSalesCategoryId).Total[month]);

    private async Task OpenEditorAsync(
        AssessmentSalesCategory category,
        ManufacturingInventoryAspect aspect)
    {
        await OffCanvasService.ShowAsync(new CanvasRequest
        {
            Title = $"{category.AssessmentSalesCategoryName} — {AspectLabel(aspect)}",
            ComponentType = typeof(AssessmentManufacturingCategoryEditor),
            Width = 1100,
            Parameters = new
            {
                AssessmentId,
                category.AssessmentSalesCategoryId,
                Aspect = aspect
            },
            ResultCallback = HandleEditorResultAsync
        });
    }

    private async Task HandleEditorResultAsync(SaveResult result)
    {
        if (result.Success)
        {
            Toast.ShowSuccess(result.Message, SessionService.AppTitle);
            if (result.RefreshGrid)
            {
                await LoadAsync();
                StateHasChanged();
            }
        }
        else if (!result.Cancelled)
        {
            Toast.ShowError(result.Message, SessionService.AppTitle);
        }
    }

    private static string AspectLabel(ManufacturingInventoryAspect aspect) => aspect switch
    {
        ManufacturingInventoryAspect.RawMaterials => "Raw Materials",
        ManufacturingInventoryAspect.WorkInProgress => "Work in Progress (WIP)",
        ManufacturingInventoryAspect.FinishedProducts => "Finished Products",
        _ => throw new ArgumentOutOfRangeException(nameof(aspect))
    };

    private string Money(decimal amount) =>
        DisplayUtility.FormatNumber(amount, SessionService.CultureName);

    private sealed record CategoryInventory(
        decimal[] RawMaterials,
        decimal[] WorkInProgress,
        decimal[] FinishedProducts,
        decimal[] Total);
}

public enum ManufacturingInventoryAspect
{
    RawMaterials,
    WorkInProgress,
    FinishedProducts
}
