using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Services;


namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentVATPage
    {
        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] IProjectionStateManager? projectionStateManager { get; set; }
        [Inject] ILogger<AssessmentSalesPage>? Logger { get; set; }


        #region Parameters
        [Parameter] public long AssessmentId { get; set; } = 1;    
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }
        #endregion

        private VatAdjustmentComponent? adjustmentOffCanvas;

        // Sample Data Arrays for 12 Months
        private decimal[] Sales = { 150000, 165000, 180000, 170000, 190000, 200000, 210000, 195000, 215000, 220000, 230000, 250000 };
        private decimal[] Purchases = { 80000, 85000, 95000, 90000, 100000, 105000, 110000, 100000, 110000, 115000, 120000, 130000 };
        private decimal[] Expenses = { 30000, 30000, 35000, 32000, 35000, 38000, 40000, 37000, 39000, 41000, 42000, 45000 };

        private decimal[] CalcOutput => Sales.Select(s => s * 0.15m).ToArray();
        private decimal[] CalcInput => Purchases.Select(p => p * 0.15m).ToArray();
        private decimal[] CalcNet => CalcOutput.Zip(CalcInput, (o, i) => o - i).ToArray();

        // Adjusted arrays (with sample overrides for demo)
        private decimal[] AdjOutput => CalcOutput.Select((val, idx) => idx == 1 ? val + 500m : val).ToArray();
        private decimal[] AdjInput => CalcInput.Select((val, idx) => idx == 2 ? val - 250m : val).ToArray();
        private decimal[] AdjNet => AdjOutput.Zip(AdjInput, (o, i) => o - i).ToArray();

        private bool IsLoading { get; set; } = true;
        private bool blAlert { get; set; } = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity { get; set; } = ViqAlertComponent.AlertSeverity.Warning;
        private string AlertHeading { get; set; } = "SALES:";
        private string AlertMessage { get; set; } = "Supply income/revenue details in this section.";


        protected override async Task OnInitializedAsync()
        {
            try
            {
                AssessmentId = sessionService.AssessmentId ?? 0;

                Logger.LogInformation(
                    "AssessmentSalesPage initialized for assessment {AssessmentId}",
                    AssessmentId);

                //await LoadAndMapSalesData();
                //await CreateSummaries();
                IsLoading = false;

                // Subscribe to projection changes
                projectionStateManager.ProjectionChanged += OnProjectionChanged;

                Logger.LogDebug("AssessmentSalesPage subscribed to ProjectionChanged events");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing AssessmentSalesPage");
                IsLoading = false;
            }
        }


        private async Task HandleSubmitData()
        {
            try
            {
                // Execute your SQL Stored Procedures or API services here
                // e.g., await ExpenseService.SaveAsync(AssessmentId, model);

                var result = new SaveResult
                {
                    Success = true,
                    ClosePanel = true, // true closes drawer, false keeps it open for more data entry
                    Message = "Configuration benchmarks updated successfully."
                };

                // Fire event back to main orchestrator page
                await OnSaveComplete.InvokeAsync(result);
            }
            catch (Exception ex)
            {
                await OnSaveComplete.InvokeAsync(new SaveResult
                {
                    Success = false,
                    Message = $"Save aborted: {ex.Message}"
                });
            }
        }


        private void OpenAdjustmentOffCanvas()
        {
            adjustmentOffCanvas?.Show();
        }

        private void HandleAdjustmentsSaved()
        {
            // Refresh table or recompute values after offcanvas saves
        }

        private void ExportReport() { }
        private void SaveChanges() { }

        private void OnProjectionChanged(object sender, ProjectionChangedEventArgs e)
        {
            if (e.AssessmentId == AssessmentId && e.DataType == "VAT")
            {
                Logger.LogInformation(
                    "VAT Adjusted, refreshing data for assessment {AssessmentId}",
                    AssessmentId);

                //InvokeAsync(async () => await LoadAndMapSalesData());
            }
        }
    }
}