using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.AssetFormComponents;

public partial class AssessmentAssetDetailComponent : ComponentBase
{
    [Parameter] public long AssessmentId { get; set; }
    [Parameter] public long AssessmentAssetId { get; set; }

    [Inject] private IGenericDataRepository<AssessmentAsset> AssetRepository { get; set; } = default!;
    [Inject] private IGenericDataRepository<AssessmentAssetMovement> MovementRepository { get; set; } = default!;
    [Inject] private MasterDataService MasterDataService { get; set; } = default!;
    [Inject] private ISessionService? sessionService { get; set; }
    [Inject] private ZabOffCanvasService OffCanvasService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private ILogger<AssessmentAssetDetailComponent> Logger { get; set; } = default!;

    private bool IsLoading { get; set; } = true;
    private AssessmentAsset? Asset { get; set; }
    private AssessmentAssetDto? AssetDto { get; set; }
    private AssessmentAssetMovement? Movement { get; set; }
    private List<AssetDetailMonth> Months { get; set; } = new();

    private decimal ClosingNetBookValue =>
        Months.LastOrDefault(month => month.IsActive)?.ClosingNetBookValue ?? 0m;

    private decimal TotalDepreciation =>
        Months.Where(month => month.IsActive).Sum(month => month.Depreciation);

    private int MovementCount => Months.Count(month => month.HasMovement);

    protected override async Task OnParametersSetAsync()
    {
        AssessmentId = AssessmentId > 0
            ? AssessmentId
            : sessionService?.AssessmentId ?? 0;

        if (AssessmentId <= 0)
        {
            Logger.LogWarning(
                "AssessmentAssetDetailComponent could not resolve the current assessment for asset {AssessmentAssetId}",
                AssessmentAssetId);
            Asset = null;
            Movement = null;
            Months = new();
            IsLoading = false;
            return;
        }

        await LoadAssetAsync();
    }

    private async Task LoadAssetAsync()
    {
        try
        {
            IsLoading = true;

            var assetTask = AssetRepository.GetAllAsync(asset =>
                asset.AssessmentAssetId == AssessmentAssetId &&
                asset.AssessmentId == AssessmentId);
            var movementTask = MovementRepository.GetAllAsync(movement =>
                movement.AssessmentAssetId == AssessmentAssetId &&
                movement.AssessmentId == AssessmentId &&
                movement.Active);
            var dtoTask = MasterDataService.GetListAsync<AssessmentAssetDto>(
                "vw_assessment_asset_list",
                new { AssessmentAssetId, AssessmentId },
                "AssetName");

            await Task.WhenAll(assetTask, movementTask, dtoTask);

            Asset = (await assetTask).FirstOrDefault();
            Movement = (await movementTask)
                .OrderByDescending(movement => movement.ModifiedDate)
                .FirstOrDefault();
            AssetDto = (await dtoTask).FirstOrDefault();

            Months = Asset == null
                ? new()
                : BuildMonths(Asset, Movement);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Error loading asset detail component for asset {AssessmentAssetId}",
                AssessmentAssetId);
            Toast.ShowError($"Could not load asset details: {ex.Message}", "Asset Details");
            Asset = null;
            Movement = null;
            Months = new();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static List<AssetDetailMonth> BuildMonths(
        AssessmentAsset asset,
        AssessmentAssetMovement? movement)
    {
        var months = new List<AssetDetailMonth>(12);
        decimal previousClosingNetBookValue = asset.OpeningNetBookValue;

        for (var month = 1; month <= 12; month++)
        {
            var isActive = asset.IsActiveInMonth(month);
            var isAcquisitionMonth = asset.IsPreExisting ? month == 1 : month == asset.AcquisitionStartMonth;

            if (!isActive)
            {
                months.Add(AssetDetailMonth.Inactive(month));
                continue;
            }

            var openingNetBookValue = isAcquisitionMonth
                ? asset.OpeningNetBookValue
                : previousClosingNetBookValue;
            var movementType = movement?.GetMovementType(month);
            var signedMovementValue = GetSignedMovementValue(
                movementType,
                movement?.GetMovementValue(month) ?? 0m);
            var depreciation = movement?.GetDepreciation(month) ?? 0m;
            var closingNetBookValue = movement?.GetNetBookValue(month)
                ?? Math.Max(0m, openingNetBookValue + signedMovementValue - depreciation);

            months.Add(new(
                month,
                true,
                isAcquisitionMonth,
                openingNetBookValue,
                movementType,
                signedMovementValue,
                depreciation,
                closingNetBookValue));

            previousClosingNetBookValue = closingNetBookValue;
        }

        return months;
    }

    private static decimal GetSignedMovementValue(string? movementType, decimal movementValue) =>
        movementType switch
        {
            "Addition" => Math.Abs(movementValue),
            "Disposal" => -Math.Abs(movementValue),
            "Transfer" => 0m,
            "Revaluation" => movementValue,
            _ => 0m
        };

    private string GetAcquisitionDisplay()
    {
        if (Asset == null)
        {
            return "Not available";
        }

        return Asset.IsPreExisting ? "Pre-existing" : $"Month {Asset.AcquisitionStartMonth}";
    }

    private static string GetMovementClass(string? movementType) =>
        movementType == "Disposal" ? "aadc-negative-movement" : "aadc-positive-movement";

    private static string DisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Not recorded" : value;

    private static string FormatAmount(decimal value) =>
        value == 0 ? "0" : value.ToString("N0").Replace(",", " ");

    private static string FormatSignedAmount(decimal value) =>
        value > 0
            ? $"+{FormatAmount(value)}"
            : value < 0
                ? $"-{FormatAmount(Math.Abs(value))}"
                : "0";

    private static string FormatCurrency(decimal value) => $"R {FormatAmount(value)}";

    private async Task CloseAsync()
    {
        await OffCanvasService.HideAsync(SaveResult.Cancel());
    }

    private sealed record AssetDetailMonth(
        int Month,
        bool IsActive,
        bool IsAcquisitionMonth,
        decimal OpeningNetBookValue,
        string? MovementType,
        decimal SignedMovementValue,
        decimal Depreciation,
        decimal ClosingNetBookValue)
    {
        public bool HasMovement =>
            !string.IsNullOrWhiteSpace(MovementType) &&
            (SignedMovementValue != 0 || MovementType == "Transfer");

        public static AssetDetailMonth Inactive(int month) =>
            new(month, false, false, 0m, null, 0m, 0m, 0m);
    }
}
