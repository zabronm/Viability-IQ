using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class SalesCategoryListComponent
    {
        [Inject] private ZabOffCanvasService zabOffCanvasService { get; set; } = default!;
        [Inject] private ToastService _Toast { get; set; } = default!;
        [Inject] private ISessionService? sessionService { get; set; }
                
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<long> OnEditRequested { get; set; }


        private long ActiveAssessmentId { get; set; }
        private string ActivePanelTitle { get; set; } = string.Empty;
        private Type? ActiveFormType { get; set; }
        private Dictionary<string, object> ActiveFormParameters { get; set; } = new();

        private IEnumerable<AssessmentSalesCategory> Categories { get; set; } = Enumerable.Empty<AssessmentSalesCategory>();
        private bool IsComponentLoading { get; set; } = true;
        private string DebugLogMessage { get; set; } = "Initializing lifecycle...";

        protected override async Task OnParametersSetAsync()
        {
            ActiveAssessmentId = AssessmentId;
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


        async Task OpenSalesCategory(long selectedId)
        {
            try
            {
                ActivePanelTitle = selectedId == 0 ?
                        "Add Sales Category" : "Edit Sales Category";

                await zabOffCanvasService.ShowAsync(
                    new CanvasRequest
                    {
                        Title = ActivePanelTitle,
                        Width = 350,
                        ComponentType = typeof(SalesCategoryFormComponent),
                        Parameters = new
                        {
                            AssessmentSalesCategoryId = selectedId,
                            AssessmentId = ActiveAssessmentId,
                            OnSaveComplete = EventCallback.Factory.Create<SaveResult>(this, (result) => RefreshComponentData(result)),
                        }
                    });
            }
            catch (Exception)
            {
                throw;
            } 
        }


        private async Task RefreshComponentData(SaveResult saveResult)
        {
            if (saveResult.Success)
            {
                _Toast.ShowSuccess(saveResult.Message, sessionService!.AppTitle);                
                 await LoadSalesCategoriesAsync();                   
                StateHasChanged();
            }
            else
            {
                _Toast.ShowError(saveResult.Message, sessionService!.AppTitle);
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