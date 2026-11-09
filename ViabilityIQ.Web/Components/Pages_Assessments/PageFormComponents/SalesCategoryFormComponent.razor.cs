using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;


namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class SalesCategoryFormComponent
    {
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public long AssessmentSalesCategoryId { get; set; }
        [Parameter] public EventCallback<SaveResult> OnSaveComplete { get; set; }
        [Parameter] public bool AllowIncomeTypeSelection { get; set; }

        [Inject] private ISessionService sessionService { get; set; } = default!;
        [Inject] private ZabOffCanvasService ZabCanvasService { get; set; } = default!;
        [Inject] private IGenericDataRepository<AssessmentSalesCategory> DataRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<AssessmentSales> salesRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<AssessmentStock> stockRepository { get; set; } = default!;


        private AssessmentSalesCategory? Model { get; set; }
        private AssessmentSales? SalesModel { get; set; }
        private AssessmentStock? StockModel { get; set; }

        private bool IsLoading { get; set; } = true;
        private bool IsSubmitting { get; set; } = false;


        protected override async Task OnInitializedAsync()
        {
            await HydrateFormStateDataAsync();
        }

        private async Task HydrateFormStateDataAsync()
        {
            try
            {
                IsLoading = true;

                if (AssessmentSalesCategoryId > 0)
                {
                    // Mode A: EDIT mode configuration target load
                    var existingRecord = await DataRepository.GetByIdAsync(AssessmentSalesCategoryId);
                    if (existingRecord != null)
                    {
                        Model = existingRecord;
                    }
                    else
                    {
                        await PublishResultAsync(SaveResult.Failed(
                            $"Sales category #{AssessmentSalesCategoryId} could not be found."));
                    }
                }
                else
                {
                    // Mode B: CREATE mode clean canvas allocation blueprint initialization
                    Model = new AssessmentSalesCategory
                    {
                        IncomeTypeId = 1,      //Default to Sales type
                        AssessmentSalesCategoryId = 0, // Signals repo to trigger an automated SQL INSERT statement
                        AssessmentId = AssessmentId,
                        AssessmentSalesCategoryName = string.Empty,
                        MarkupPercentage = 0x00000000, // Explicit clean decimal baseline initialization
                        Active = true,
                        Remarks = string.Empty,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                await PublishResultAsync(SaveResult.Failed(
                    $"Unable to load the sales category: {ex.Message}"));
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task ExecuteSaveWorkflow()
        {
            SaveResult executionFeedbackPackage;

            if (Model == null || IsSubmitting)
            {
                return;
            }

            try
            {
                IsSubmitting = true;
                bool isNewCategory = Model.AssessmentSalesCategoryId == 0 ? true : false;

                bool isExecutionSuccess = await DataRepository.SaveAsync(Model);
                if (isExecutionSuccess)
                {
                    //==== INITIATE THE SALES CATEGORY INTO SALES AND INVENTORY
                    if (isNewCategory)
                    {
                        //write this record into sales if its new
                        SalesModel = new()
                        {
                            AssessmentSalesId = 0, // Signals repo to trigger an automated SQL INSERT statement
                            AssessmentId = Model.AssessmentId,
                            ProductCategoryId = Model.AssessmentSalesCategoryId,
                            Description = Model.AssessmentSalesCategoryName,
                            IncomeTypeId = Model.IncomeTypeId,
                            CreatedDate = DateTime.Now,
                            CreatedBy = sessionService.UserId,
                        };

                        isExecutionSuccess = await salesRepository.SaveAsync(SalesModel);
                        if (!isExecutionSuccess)
                        {
                            throw new InvalidOperationException(
                                "The sales category was created, but its income stream could not be initialized.");
                        }

                        //Initialise Stock record for this category
                        StockModel = new()
                        {
                            AssessmentId = Model.AssessmentId,
                            Active = true,
                            Description = Model.AssessmentSalesCategoryName ?? string.Empty,
                            AssessmentSalesCategoryId = Model.AssessmentSalesCategoryId,
                            Remarks = Model.Remarks,
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = sessionService.UserId
                        };
                        isExecutionSuccess = await stockRepository.SaveAsync(StockModel);
                        if (!isExecutionSuccess)
                        {
                            throw new InvalidOperationException(
                                "The sales category and income stream were created, but its inventory record could not be initialized.");
                        }
                    }
                }

                executionFeedbackPackage = new()
                {
                    Success = isExecutionSuccess,
                    ClosePanel = isExecutionSuccess,
                    Message = isExecutionSuccess
                        ? "Sales category details committed successfully."
                        : "Error encountered; sales category not saved."
                };

                await PublishResultAsync(executionFeedbackPackage);

            }
            catch (Exception ex)
            {
                await PublishResultAsync(SaveResult.Failed(
                    $"Unable to save the sales category: {ex.Message}"));
            }
            finally
            {

                IsSubmitting = false;
            }
        }

        private async Task PublishResultAsync(SaveResult result)
        {
            if (OnSaveComplete.HasDelegate)
            {
                await OnSaveComplete.InvokeAsync(result);
            }
            else
            {
                await ZabCanvasService.PublishResultAsync(result);
            }

            if (result.ClosePanel)
            {
                await ZabCanvasService.CloseAsync();
            }
        }
    }
}