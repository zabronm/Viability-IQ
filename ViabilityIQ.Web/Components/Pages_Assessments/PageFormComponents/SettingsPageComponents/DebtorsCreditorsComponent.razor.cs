using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents.SettingsPageComponents;

public partial class DebtorsCreditorsComponent
{
    [Inject] private IGenericDataRepository<DebtorsCreditorsProfile> ProfileRepository { get; set; } = default!;
    [Inject] private MasterDataService ViqCrudService { get; set; } = default!;
    [Inject] private ILogger<DebtorsCreditorsComponent> Logger { get; set; } = default!;

    [Parameter] public long AssessmentId { get; set; }
    [Parameter] public EventCallback<SaveResult> OnUpdate { get; set; }

    public DebtorsCreditorsProfile? debtorsCreditorsModel { get; set; }

    private static readonly IReadOnlyList<AgeBucket> AgeBuckets =
    [
        new(0, "0–30 days", "Current"),
        new(1, "31–60 days", "One month overdue"),
        new(2, "61–90 days", "Two months overdue"),
        new(3, "91–120 days", "Three months overdue"),
        new(4, "120+ days", "Long outstanding")
    ];

    private readonly List<string> ValidationMessages = [];
    private bool IsSubmitting;
    private bool loadingStateActive;
    private long _loadedAssessmentId;

    private string ModeDescription => debtorsCreditorsModel?.EntryMode == true
        ? "Enter the known Rand balances; percentages are calculated automatically."
        : "Enter the expected percentage distribution used by projections.";

    private decimal TotalCreditorsValue => SumValues(false);
    private decimal TotalDebtorsValue => SumValues(true);
    private decimal TotalCreditorsPercentage => SumPercentages(false);
    private decimal TotalDebtorsPercentage => SumPercentages(true);

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedAssessmentId == AssessmentId && debtorsCreditorsModel is not null)
            return;

        _loadedAssessmentId = AssessmentId;
        await LoadDebtorsCreditorsAsync();
    }

    public async Task RefreshAsync()
    {
        _loadedAssessmentId = 0;
        await LoadDebtorsCreditorsAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadDebtorsCreditorsAsync()
    {
        loadingStateActive = true;
        ValidationMessages.Clear();

        try
        {
            debtorsCreditorsModel = await ViqCrudService.GetSingleAsync<DebtorsCreditorsProfile>(
                "tblAssessmentDebtorsCreditorsProfile",
                new { AssessmentId })
                ?? CreateDefaultProfile();

            debtorsCreditorsModel.AssessmentId = AssessmentId;

            if (debtorsCreditorsModel.EntryMode)
            {
                ConvertValuesToPercentages(false);
                ConvertValuesToPercentages(true);
            }
            else
            {
                NormalizePercentageProfile(false);
                NormalizePercentageProfile(true);
                SyncValuesFromPercentages(false);
                SyncValuesFromPercentages(true);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading debtors/creditors profile for assessment {AssessmentId}", AssessmentId);
            debtorsCreditorsModel = CreateDefaultProfile();
            ValidationMessages.Add("The saved profile could not be loaded. Default percentages are shown; please retry before saving.");
        }
        finally
        {
            loadingStateActive = false;
        }
    }

    private DebtorsCreditorsProfile CreateDefaultProfile() => new()
    {
        AssessmentId = AssessmentId,
        Creditors_30 = 50m,
        Creditors_60 = 20m,
        Creditors_90 = 10m,
        Creditors_120 = 10m,
        Creditors_120Plus = 10m,
        Debtors_30 = 50m,
        Debtors_60 = 20m,
        Debtors_90 = 10m,
        Debtors_120 = 10m,
        Debtors_120Plus = 10m,
        BadDebtPercentage = 2m,
        EntryMode = false,
        Active = true
    };

    private void SetEntryMode(bool useActualValues)
    {
        if (debtorsCreditorsModel is null || debtorsCreditorsModel.EntryMode == useActualValues)
            return;

        ValidationMessages.Clear();

        if (useActualValues)
        {
            SyncValuesFromPercentages(false);
            SyncValuesFromPercentages(true);
        }
        else
        {
            ConvertValuesToPercentages(false);
            ConvertValuesToPercentages(true);
        }

        debtorsCreditorsModel.EntryMode = useActualValues;
    }

    private void SetValue(bool isDebtor, int bucketIndex, object? rawValue)
    {
        SetValue(isDebtor, bucketIndex, ParseNonNegativeDecimal(rawValue));
        ConvertValuesToPercentages(isDebtor);
        ValidationMessages.Clear();
    }

    private void SetPercentage(bool isDebtor, int bucketIndex, object? rawValue)
    {
        if (bucketIndex == 0)
            return;

        SetPercentage(isDebtor, bucketIndex, Clamp(ParseNonNegativeDecimal(rawValue), 0m, 100m));
        NormalizePercentageProfile(isDebtor);
        SyncValuesFromPercentages(isDebtor);
        ValidationMessages.Clear();
    }

    private void ConvertValuesToPercentages(bool isDebtor)
    {
        ClampValues(isDebtor);
        var total = SumValues(isDebtor);

        if (total <= 0m)
        {
            NormalizePercentageProfile(isDebtor);
            return;
        }

        var laterBucketTotal = 0m;
        for (var index = 1; index < AgeBuckets.Count; index++)
        {
            var percentage = decimal.Round(GetValue(isDebtor, index) / total * 100m, 4, MidpointRounding.AwayFromZero);
            SetPercentage(isDebtor, index, percentage);
            laterBucketTotal += percentage;
        }

        ReduceRoundingExcess(isDebtor, ref laterBucketTotal);
        SetPercentage(isDebtor, 0, Math.Max(0m, 100m - laterBucketTotal));
    }

    private void NormalizePercentageProfile(bool isDebtor)
    {
        var laterTotal = 0m;
        for (var index = 1; index < AgeBuckets.Count; index++)
        {
            var percentage = Clamp(GetPercentage(isDebtor, index), 0m, 100m);
            SetPercentage(isDebtor, index, percentage);
            laterTotal += percentage;
        }

        if (laterTotal > 100m)
        {
            var scale = 100m / laterTotal;
            laterTotal = 0m;

            for (var index = 1; index < AgeBuckets.Count; index++)
            {
                var percentage = decimal.Round(
                    GetPercentage(isDebtor, index) * scale,
                    4,
                    MidpointRounding.AwayFromZero);
                SetPercentage(isDebtor, index, percentage);
                laterTotal += percentage;
            }
        }

        ReduceRoundingExcess(isDebtor, ref laterTotal);
        SetPercentage(isDebtor, 0, Math.Max(0m, 100m - laterTotal));
    }

    private void ReduceRoundingExcess(bool isDebtor, ref decimal laterBucketTotal)
    {
        if (laterBucketTotal <= 100m)
            return;

        var largestBucket = Enumerable.Range(1, AgeBuckets.Count - 1)
            .OrderByDescending(index => GetPercentage(isDebtor, index))
            .First();
        var excess = laterBucketTotal - 100m;
        SetPercentage(
            isDebtor,
            largestBucket,
            Math.Max(0m, GetPercentage(isDebtor, largestBucket) - excess));
        laterBucketTotal = 100m;
    }

    private void SyncValuesFromPercentages(bool isDebtor)
    {
        var total = SumValues(isDebtor);
        if (total <= 0m)
            return;

        var allocated = 0m;
        for (var index = 1; index < AgeBuckets.Count; index++)
        {
            var amount = decimal.Round(
                total * GetPercentage(isDebtor, index) / 100m,
                2,
                MidpointRounding.AwayFromZero);
            SetValue(isDebtor, index, amount);
            allocated += amount;
        }

        SetValue(isDebtor, 0, Math.Max(0m, total - allocated));
    }

    private void ClampValues(bool isDebtor)
    {
        for (var index = 0; index < AgeBuckets.Count; index++)
            SetValue(isDebtor, index, Math.Max(0m, GetValue(isDebtor, index)));
    }

    private List<string> ValidateProfile()
    {
        var messages = new List<string>();

        if (debtorsCreditorsModel is null)
            return ["The profile is unavailable."];

        if (Math.Abs(TotalCreditorsPercentage - 100m) > 0.01m)
            messages.Add("The creditors projection profile must total 100%.");

        if (Math.Abs(TotalDebtorsPercentage - 100m) > 0.01m)
            messages.Add("The debtors projection profile must total 100%.");

        if (debtorsCreditorsModel.BadDebtPercentage is < 0m or > 100m)
            messages.Add("Expected bad debts must be between 0% and 100%.");

        if (debtorsCreditorsModel.AveragePaymentDays is < 0 or > 365)
            messages.Add("Average payment days must be between 0 and 365.");

        return messages;
    }

    private async Task UpdateDebtorsCreditors()
    {
        if (debtorsCreditorsModel is null || IsSubmitting)
            return;

        ValidationMessages.Clear();

        if (debtorsCreditorsModel.EntryMode)
        {
            ConvertValuesToPercentages(false);
            ConvertValuesToPercentages(true);
        }
        else
        {
            NormalizePercentageProfile(false);
            NormalizePercentageProfile(true);
            SyncValuesFromPercentages(false);
            SyncValuesFromPercentages(true);
        }

        ValidationMessages.AddRange(ValidateProfile());
        if (ValidationMessages.Count > 0)
            return;

        SaveResult result;
        IsSubmitting = true;

        try
        {
            debtorsCreditorsModel.AssessmentId = AssessmentId;
            debtorsCreditorsModel.Active = true;
            debtorsCreditorsModel.ModifiedDate = DateTime.UtcNow;

            if (debtorsCreditorsModel.DebtorsCreditorsProfileId == 0)
                debtorsCreditorsModel.CreatedDate = DateTime.UtcNow;

            var saved = await ProfileRepository.SaveAsync(debtorsCreditorsModel);
            result = saved
                ? SaveResult.SavedAndClose("Debtors and creditors profiles updated successfully.")
                : SaveResult.Failed("The debtors and creditors profiles could not be saved. Please retry.");

            if (saved)
                Logger.LogInformation(
                    "Saved projection-ready debtors/creditors percentages for assessment {AssessmentId} from {EntryMode} mode",
                    AssessmentId,
                    debtorsCreditorsModel.EntryMode ? "actual-value" : "percentage");
            else
                Logger.LogWarning("Failed to save debtors/creditors profile for assessment {AssessmentId}", AssessmentId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving debtors/creditors profile for assessment {AssessmentId}", AssessmentId);
            result = SaveResult.Failed("An unexpected error prevented the profiles from being saved.");
        }
        finally
        {
            IsSubmitting = false;
        }

        if (OnUpdate.HasDelegate)
            await OnUpdate.InvokeAsync(result);
    }

    private decimal GetPercentage(bool isDebtor, int index)
    {
        if (debtorsCreditorsModel is null)
            return 0m;

        return (isDebtor, index) switch
        {
            (true, 0) => debtorsCreditorsModel.Debtors_30,
            (true, 1) => debtorsCreditorsModel.Debtors_60,
            (true, 2) => debtorsCreditorsModel.Debtors_90,
            (true, 3) => debtorsCreditorsModel.Debtors_120,
            (true, 4) => debtorsCreditorsModel.Debtors_120Plus,
            (false, 0) => debtorsCreditorsModel.Creditors_30,
            (false, 1) => debtorsCreditorsModel.Creditors_60,
            (false, 2) => debtorsCreditorsModel.Creditors_90,
            (false, 3) => debtorsCreditorsModel.Creditors_120,
            (false, 4) => debtorsCreditorsModel.Creditors_120Plus,
            _ => 0m
        };
    }

    private void SetPercentage(bool isDebtor, int index, decimal value)
    {
        if (debtorsCreditorsModel is null)
            return;

        switch (isDebtor, index)
        {
            case (true, 0): debtorsCreditorsModel.Debtors_30 = value; break;
            case (true, 1): debtorsCreditorsModel.Debtors_60 = value; break;
            case (true, 2): debtorsCreditorsModel.Debtors_90 = value; break;
            case (true, 3): debtorsCreditorsModel.Debtors_120 = value; break;
            case (true, 4): debtorsCreditorsModel.Debtors_120Plus = value; break;
            case (false, 0): debtorsCreditorsModel.Creditors_30 = value; break;
            case (false, 1): debtorsCreditorsModel.Creditors_60 = value; break;
            case (false, 2): debtorsCreditorsModel.Creditors_90 = value; break;
            case (false, 3): debtorsCreditorsModel.Creditors_120 = value; break;
            case (false, 4): debtorsCreditorsModel.Creditors_120Plus = value; break;
        }
    }

    private decimal GetValue(bool isDebtor, int index)
    {
        if (debtorsCreditorsModel is null)
            return 0m;

        return (isDebtor, index) switch
        {
            (true, 0) => debtorsCreditorsModel.DebtorsValue_30,
            (true, 1) => debtorsCreditorsModel.DebtorsValue_60,
            (true, 2) => debtorsCreditorsModel.DebtorsValue_90,
            (true, 3) => debtorsCreditorsModel.DebtorsValue_120,
            (true, 4) => debtorsCreditorsModel.DebtorsValue_120Plus,
            (false, 0) => debtorsCreditorsModel.CreditorsValue_30,
            (false, 1) => debtorsCreditorsModel.CreditorsValue_60,
            (false, 2) => debtorsCreditorsModel.CreditorsValue_90,
            (false, 3) => debtorsCreditorsModel.CreditorsValue_120,
            (false, 4) => debtorsCreditorsModel.CreditorsValue_120Plus,
            _ => 0m
        };
    }

    private void SetValue(bool isDebtor, int index, decimal value)
    {
        if (debtorsCreditorsModel is null)
            return;

        switch (isDebtor, index)
        {
            case (true, 0): debtorsCreditorsModel.DebtorsValue_30 = value; break;
            case (true, 1): debtorsCreditorsModel.DebtorsValue_60 = value; break;
            case (true, 2): debtorsCreditorsModel.DebtorsValue_90 = value; break;
            case (true, 3): debtorsCreditorsModel.DebtorsValue_120 = value; break;
            case (true, 4): debtorsCreditorsModel.DebtorsValue_120Plus = value; break;
            case (false, 0): debtorsCreditorsModel.CreditorsValue_30 = value; break;
            case (false, 1): debtorsCreditorsModel.CreditorsValue_60 = value; break;
            case (false, 2): debtorsCreditorsModel.CreditorsValue_90 = value; break;
            case (false, 3): debtorsCreditorsModel.CreditorsValue_120 = value; break;
            case (false, 4): debtorsCreditorsModel.CreditorsValue_120Plus = value; break;
        }
    }

    private decimal SumPercentages(bool isDebtor) =>
        Enumerable.Range(0, AgeBuckets.Count).Sum(index => GetPercentage(isDebtor, index));

    private decimal SumValues(bool isDebtor) =>
        Enumerable.Range(0, AgeBuckets.Count).Sum(index => GetValue(isDebtor, index));

    private static decimal ParseNonNegativeDecimal(object? rawValue) =>
        decimal.TryParse(
            Convert.ToString(rawValue, CultureInfo.CurrentCulture),
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out var value)
            ? Math.Max(0m, value)
            : 0m;

    private static decimal Clamp(decimal value, decimal minimum, decimal maximum) =>
        Math.Min(maximum, Math.Max(minimum, value));

    private static string FormatCurrency(decimal value) =>
        $"R {value:N2}";

    private sealed record AgeBucket(int Index, string Label, string Description);
}
