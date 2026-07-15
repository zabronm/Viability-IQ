using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class SalesCategoryListComponent
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<long> OnEditRequested { get; set; }

        private IEnumerable<AssessmentSalesCategory> Categories { get; set; } = Enumerable.Empty<AssessmentSalesCategory>();
        private bool IsComponentLoading { get; set; } = true;
        private string DebugLogMessage { get; set; } = "Initializing lifecycle...";

        protected override async Task OnParametersSetAsync()
        {
            await LoadSalesCategoriesAsync();
        }

        private async Task LoadSalesCategoriesAsync()
        {
            try
            {
                IsComponentLoading = true;
                DebugLogMessage = "Fetching repository collections...";
                StateHasChanged();

                // FORCE the database operation onto an isolated background thread pool worker to prevent deadlocks
                var rawRecords = await Task.Run(async () =>
                {
                    return await SalesCategoryRepository.GetAllAsync();
                });

                if (rawRecords != null)
                {
                    // Removed "&& x.Active" filter layer so both Active and Inactive entries load into the layout matrix safely
                    Categories = rawRecords
                        .Where(x => x.AssessmentId == AssessmentId)
                        .ToList();

                    DebugLogMessage = $"Data tracking complete. Records count: {Categories.Count()}";
                }
                else
                {
                    DebugLogMessage = "Repository data returned null payload streams.";
                    Categories = Enumerable.Empty<AssessmentSalesCategory>();
                }
            }
            catch (Exception ex)
            {
                DebugLogMessage = $"Exception caught in thread loop: {ex.Message}";
                Categories = Enumerable.Empty<AssessmentSalesCategory>();
            }
            finally
            {
                IsComponentLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        //same as below but different name in settings page
        public async Task RefreshAsync()
                => await LoadSalesCategoriesAsync();

        public async Task RefreshListAsync()
                => await LoadSalesCategoriesAsync();

      
        /// Evaluates numeric markup thresholds and returns explicit design presentation tokens        
        private string GetMarkupBadgeClass(decimal markupPercent) => markupPercent switch
        {
            < 5.00m => "btn-danger text-black",
            >= 5.00m and < 15.00m => "btn-warning text-dark",
            >= 15.00m and < 30.00m => "bg-info text-dark",
            >= 30.00m and <= 50.00m => "bg-success text-dark",
            _ => "bg-white text-dark border border-danger" // > 50 design block setup
        };
    }
}