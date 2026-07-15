using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
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
        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ToastService? _Toast { get; set; }

        [Parameter] public long AssessmentId { get; set; }
        

        private AssessmentFinancialsDto ConsolidatedAssessmentData { get; set; } = new();
        private List<UnifiedIncomeViewModel> IncomeStreams { get; set; } = new();
        private bool IsLoading { get; set; } = true;
        private bool blAlert { get; set; } = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity { get; set; } = ViqAlertComponent.AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "SALES:";
        private string AlertMessage { get; set; } = "Supply income/revenue details in this section.";

        private IncomeTypeEnum? SelectedFilterType { get; set; }
        private string SearchQuery { get; set; } = string.Empty;
        private long SelectedFilterId { get; set; } = 0;
        private decimal GrandTotalRevenue => FilteredIncomeStreams?.Sum(c => c.MonthlyValues.Sum()) ?? 0;

        //private IEnumerable<UnifiedIncomeViewModel> FilteredIncomeStreams =>
        //    IncomeStreams.Where(x => (string.IsNullOrWhiteSpace(SearchQuery) || x.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) &&
        //                             (!SelectedFilterType.HasValue || x.Type == SelectedFilterType.Value));
               
        private IEnumerable<UnifiedIncomeViewModel> FilteredIncomeStreams =>
                            IncomeStreams.Where(x =>
                            (string.IsNullOrWhiteSpace(SearchQuery) || x.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) &&
                            (SelectedFilterId == 0 || (long)x.TypeId == SelectedFilterId));

        protected override async Task OnInitializedAsync()
        {
            AssessmentId = sessionService.AssessmentId ?? 0;
            //AssessmentId = sessionService!.AssessmentId.Value;
            await LoadAndMapSalesData();
            await CreateSummaries();
            IsLoading = false;
        }

        async Task LoadAndMapSalesData()
        {
            var result = await ViqCrudService!.GetListAsync<AssessmentSalesDto>("vw_assessment_sales_list",
                new { AssessmentId }, "AssessmentSalesId");

            if (result != null)
            {
                IncomeStreams = result.Select(s => new UnifiedIncomeViewModel
                {                     
                    Id = s.AssessmentSalesId,
                    Description = s.Description ?? "N/A",
                    TypeId = (long)s.IncomeTypeId,          // Direct cast
                    TypeName = s.IncomeTypeName ?? "N/A",   // Direct map
                    IncludesVat = s.IncludeVAT > 0,
                    MonthlyValues = new decimal[] { s.Month_1, s.Month_2, s.Month_3, s.Month_4, s.Month_5, s.Month_6,
                                                    s.Month_7, s.Month_8, s.Month_9, s.Month_10, s.Month_11, s.Month_12 }
                }).ToList();
            }
        }

        private async Task CreateSummaries()
        {
            // Define the list of IDs that you consider to be "Sales"
            // Replace 1 and 2 with the actual IDs from your database lookups
            var salesTypeIds = new List<long> { 1, 2 };

            ConsolidatedAssessmentData.MonthlySales = IncomeStreams
                .Where(x => salesTypeIds.Contains(x.TypeId))
                .Aggregate(new decimal[12], (acc, cur) =>
                {
                    for (int i = 0; i < 12; i++) acc[i] += cur.MonthlyValues[i];
                    return acc;
                });

            await Task.CompletedTask;
            //ConsolidatedAssessmentData.MonthlySales = IncomeStreams.Where(x => x.Inco == IncomeTypeEnum.SalesCategoryA || x.Type == IncomeTypeEnum.SalesCategoryB)
            //    .Aggregate(new decimal[12], (acc, cur) => { for (int i = 0; i < 12; i++) acc[i] += cur.MonthlyValues[i]; return acc; });
        }

        private async Task AddIncomeStream() => await OpenIncomeFormPanel(new UnifiedIncomeViewModel());

        private async Task OpenIncomeFormPanel(UnifiedIncomeViewModel stream)
        {
            await zabCanvasService!.ShowAsync(new CanvasRequest
            {
                Title = stream.Id == 0 ? "Add Revenue Stream" : "Edit Revenue Stream",
                Width = 400,
                ComponentType = typeof(IncomeSalesFormComponent),
                Parameters = new
                {
                    IncomeContext = stream
                },
                ResultCallback = OnSaveComplete
            });
        }


        //=================  IMPLEMENT CALL BACK SO YOU READ THE SAVERESULT OBJECT ====================
        async Task OnSaveComplete(SaveResult result)
        {
            if (result.Success)
            {
                _Toast!.ShowSuccess(result.Message, sessionService!.AppTitle);
                if (result.RefreshGrid)
                    await LoadAndMapSalesData();

            }
            else
            {
                _Toast!.ShowError(result.Message, sessionService!.AppTitle);
            }
        
        }



    }
}