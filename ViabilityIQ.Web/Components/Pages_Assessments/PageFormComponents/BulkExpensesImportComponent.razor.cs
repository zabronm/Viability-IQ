using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class BulkExpensesImportComponent
    {
        [Inject] private IGenericDataRepository<ExpenseItems> ExpenseItemsRepository { get; set; } = default!;
        [Inject] private IGenericDataRepository<AssessmentExpenses> AssessmentExpenseRepository { get; set; } = default!;
        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Parameter] public long AssessmentId { get; set; }

        private List<ExpenseItems> expenseItemsList = new();
        private List<BulkExpenseItem> AvailableExpenses = new();

        private int SelectedCount => AvailableExpenses.Count(x => x.IsSelected);
        bool isSubmitting = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var resultSet = (await ExpenseItemsRepository.GetAllAsync());
                expenseItemsList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<ExpenseItems>();

                var existing = await ViqCrudService!.GetListAsync<AssessmentExpenses>("tblAssessmentExpenses", new { AssessmentId }, "AssessmentId");

                var existingIds = existing.Select(e => e.ExpenseItemId).ToHashSet();

                AvailableExpenses = expenseItemsList.Where(i => !existingIds.Contains(i.ExpenseItemId))
                                            .Select(i => new BulkExpenseItem { ExpenseItemId = i.ExpenseItemId, ExpenseItemName = i.ExpenseItemName })
                                            .ToList();
            }
            catch (Exception ex)
            {
                await zabCanvasService.PublishResultAsync(new SaveResult
                {
                    Success = false,
                    Message = "Error encountered: " + ex.Message,
                });
            }
        }


        private async Task ImportSelected()
        {
            try
            {
                isSubmitting = true;

                var toImport = AvailableExpenses.Where(x => x.IsSelected).ToList();// Check for validation errors

                //======= Validate here => All selected items must have a valid ExpenseTypeId (greater than 0)
                if (toImport.Any(x => x.ExpenseTypeId <= 0))
                {
                    // Replace this with your specific toast or alert service
                    await zabCanvasService!.PublishResultAsync(new SaveResult
                    {
                        Success = false,
                        Message = "Please select an [Expense Type] for all selected items."
                    });
                    return;
                }


                foreach (var item in toImport)
                {
                    await AssessmentExpenseRepository.SaveAsync(new AssessmentExpenses
                    {
                        AssessmentId = AssessmentId,
                        ExpenseItemId = item.ExpenseItemId,
                        blSendToCashBook = item.PostToCashbook,
                        blPercentageOfSalesUsed = item.UsePercentageOfSales,
                        ExpenseTypeId = item.ExpenseTypeId,
                        Description = item.ExpenseItemName
                    });
                }

                // Pass the count back in the success message
                await zabCanvasService.PublishResultAsync(SaveResult.SavedAndClose($"Successfully imported {toImport.Count} expenses."));

            }
            catch (Exception ex)
            {
                await zabCanvasService.PublishResultAsync(new SaveResult
                {
                    Success = false,
                    Message = "Error encountered: " + ex.Message,
                });
            }
            finally
            {
                isSubmitting = false;
            }
        }


        public class BulkExpenseItem
        {
            public long ExpenseItemId { get; set; }
            public string? ExpenseItemName { get; set; }
            public bool IsSelected { get; set; }
            public bool PostToCashbook { get; set; }
            public bool UsePercentageOfSales { get; set; }
            public long ExpenseTypeId { get; set; }

            // UI Validation helper
            public bool IsValid => !IsSelected || ExpenseTypeId > 0;
        }

    }
}
