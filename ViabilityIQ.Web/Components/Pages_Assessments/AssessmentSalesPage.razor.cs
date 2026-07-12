using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.DataModels.FinCalculations;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentSalesPage
    {
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ToastService? _Toast { get; set; }

        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }

        
        private AssessmentFinancialsDto ConsolidatedAssessmentData { get; set; } = new(); // EXTREMELY IMPORTANT=>PASSES PARAMETERS TO THE FINANCIAL SUMMARY COMPONENTS =================
        private bool AreSummariesReady { get; set; } = false;                             //Transition animation


        private string ActivePanelTitle = string.Empty;
        private bool blAlert { get; set; } = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity { get; set; } = ViqAlertComponent.AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "Sales Notice:";
        private string AlertMessage { get; set; } = "Verify that your sales values align accurately with your cost of sales allocations.";

        private string SearchQuery { get; set; } = string.Empty;
        private IncomeTypeEnum? SelectedFilterType { get; set; }

        private List<UnifiedIncomeViewModel> IncomeStreams { get; set; } = new();

        private IEnumerable<UnifiedIncomeViewModel> FilteredIncomeStreams =>
            IncomeStreams.Where(x =>
                (string.IsNullOrWhiteSpace(SearchQuery) || x.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) &&
                (!SelectedFilterType.HasValue || x.Type == SelectedFilterType.Value));

        private decimal GrandTotalRevenue => FilteredIncomeStreams?.Sum(c => c.MonthlyValues.Sum()) ?? 0;

        protected override async Task OnInitializedAsync()
        {
            SeedWorkspaceMatrixPipeline();
            await  CreateSummaries();
        }

        private void SeedWorkspaceMatrixPipeline()
        {
            IncomeStreams = new List<UnifiedIncomeViewModel>
            {
                new UnifiedIncomeViewModel { Id = 101, Description = "Product Category A - Retail Operations", Type = IncomeTypeEnum.SalesCategoryA, IncludesVat = true, MonthlyValues = new decimal[12] { 12000, 14000, 15000, 13500, 16000, 17500, 16500, 18000, 19500, 18500, 21000, 24000 } },
                new UnifiedIncomeViewModel { Id = 102, Description = "Product Category B - Wholesale Enterprise", Type = IncomeTypeEnum.SalesCategoryB, IncludesVat = false, MonthlyValues = new decimal[12] { 8500, 9200, 11000, 10500, 12000, 11500, 13000, 12500, 14000, 15500, 16000, 18500 } },
                new UnifiedIncomeViewModel { Id = 201, Description = "Rental Property Auxiliary Inflow", Type = IncomeTypeEnum.SundryIncome, IncludesVat = false, MonthlyValues = new decimal[12] { 4500, 4500, 4500, 4500, 4500, 4500, 4800, 4800, 4800, 4800, 4800, 4800 } },
                new UnifiedIncomeViewModel { Id = 501, Description = "SEDA Technology Incubator Funding Support", Type = IncomeTypeEnum.GrantsDonations, IncludesVat = false, MonthlyValues = new decimal[12] { 0, 0, 25000, 0, 0, 0, 25000, 0, 0, 0, 0, 50000 } }
            };
        }

        private string GetDisplayTypeName(IncomeTypeEnum type) => type switch
        {
            IncomeTypeEnum.SalesCategoryA => "Sales Category A",
            IncomeTypeEnum.SalesCategoryB => "Sales Category B",
            IncomeTypeEnum.SundryIncome => "Sundry Income",
            IncomeTypeEnum.GrantsDonations => "Grants/Donations",
            _ => type.ToString()
        };

        async Task AddIncomeStream() => await OpenIncomeFormPanel(new UnifiedIncomeViewModel());

        private async Task OpenIncomeFormPanel(UnifiedIncomeViewModel stream)
        {
            ActivePanelTitle = stream.Id == 0 ? "Add Revenue Stream" : "Edit Revenue Stream";

            await zabCanvasService!.ShowAsync(
                new CanvasRequest
                {
                    Title = ActivePanelTitle,
                    Width = 380,
                    ComponentType = typeof(IncomeSalesFormComponent),
                    Parameters = new { IncomeContext = stream },
                    ResultCallback = HandleIncomeResultAsync
                });
        }

        private async Task HandleIncomeResultAsync(SaveResult result)
        {
            if (result.Success && result.Data is UnifiedIncomeViewModel updatedModel)
            {
                var match = IncomeStreams.FirstOrDefault(x => x.Id == updatedModel.Id && x.Id != 0);
                if (match != null)
                {
                    match.Description = updatedModel.Description;
                    match.Type = updatedModel.Type;
                    match.IncludesVat = updatedModel.IncludesVat;
                    match.MonthlyValues = updatedModel.MonthlyValues;
                }
                else
                {
                    updatedModel.Id = IncomeStreams.Max(x => x.Id) + 1;
                    IncomeStreams.Add(updatedModel);
                }

                await TriggerParentCompleteAsync();
                _Toast!.ShowSuccess("Data saved successfully", sessionService!.AppTitle);
            }
        }

        private async Task TriggerParentCompleteAsync()
        {
            StateHasChanged();
            if (OnSaveComplete.HasDelegate)
            {
                await OnSaveComplete.InvokeAsync(new SaveResult { Success = true });
            }
        }


        //=============================== PROCEDURE TO CREATE AND INITIATE SUMMARY COMPONENTS
        async Task CreateSummaries()
        {
            //SeedWorkspaceMatrixPipeline();
            MapCalculationsToSummaryPayload();
            await Task.Delay(350);
            AreSummariesReady = true;
        }

        private void MapCalculationsToSummaryPayload()
        {
            // Bind your current list arrays into the matching summary elements
            ConsolidatedAssessmentData.MonthlySales = IncomeStreams
                .Where(x => x.Type == IncomeTypeEnum.SalesCategoryA || x.Type == IncomeTypeEnum.SalesCategoryB)
                .Aggregate(new decimal[12], (acc, cur) => {
                    for (int i = 0; i < 12; i++) acc[i] += cur.MonthlyValues[i];
                    return acc;
                });

            ConsolidatedAssessmentData.MonthlySundryIncome = IncomeStreams
                .Where(x => x.Type == IncomeTypeEnum.SundryIncome || x.Type == IncomeTypeEnum.GrantsDonations)
                .Aggregate(new decimal[12], (acc, cur) => {
                    for (int i = 0; i < 12; i++) acc[i] += cur.MonthlyValues[i];
                    return acc;
                });

            // Dummy Mock values for auxiliary fields - these will map to your real tables/services
            ConsolidatedAssessmentData.MonthlyCostOfSales = new decimal[12] { 4000, 4500, 5000, 4800, 5200, 6000, 5500, 5800, 6200, 6100, 7000, 8000 };
            ConsolidatedAssessmentData.MonthlyExpenses = new decimal[12] { 2500, 2500, 2700, 2600, 2600, 2900, 2800, 2800, 3000, 3100, 3200, 3500 };
            ConsolidatedAssessmentData.TotalFixedCosts = 32000m;
            ConsolidatedAssessmentData.TotalFixedAssets = 150000m;
            ConsolidatedAssessmentData.AverageStockValue = 12500m;
        }
    }
}