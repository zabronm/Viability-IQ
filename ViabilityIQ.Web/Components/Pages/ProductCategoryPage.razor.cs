

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Data;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Services;
using static Microsoft.Data.SqlClient.Internal.SqlClientEventSource;

namespace ViabilityIQ.Web.Components.Pages
{
    public partial class ProductCategoryPage
    {
        [Inject] private IGenericDataRepository<ProductCategoryDto> productCategoryRepository { get; set; } = default!;
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IPdfExportService PdfService { get; set; } = default!;
        [Inject] private IExcelEPPlusExportService ExcelService { get; set; } = default!;
        //[Inject] private IEmailReportingService EmailService { get; set; } = default!;      

        // Alert
        //---------------------------------------------------------
        private bool blAlert = true;
        private ViqAlertComponent.AlertSeverity AlertSeverity = ViqAlertComponent.AlertSeverity.Info;
        private string AlertHeading = "Product Categories";
        private string AlertMessage = "Register your all your company's product/service categories under which your sales revenue will be classified, for example Clothing, Groceries, Alcohol, etc";

        private List<ProductCategoryDto> productCategoryList = new();
        private List<ZabDataTableAdvanced<ProductCategoryDto>.ColumnDefinition<ProductCategoryDto>> tableColumns = new();

        // State Machine parameters for modal canvas controls
        private ZabOffCanvas? canvasShell;
        private bool canvasOpenStatus = false;
        private string formTitle = "Product Category";
        private long activeRecordId = 0;
        private bool loadingStateActive = false;

        protected override async Task OnInitializedAsync()
        {
            _ = LoadGridDatasetAsync();

            tableColumns = new List<ZabDataTableAdvanced<ProductCategoryDto>.ColumnDefinition<ProductCategoryDto>>
            {
                new() { Title = "Category Name", Value = x => x.ProductCategoryName },
                new() { Title = "Income Type", Value = x => x.IncomeTypeName },
                new() { Title = "UoM", Value = x => x.UoM },
                new() { Title = "Markup(%)", Value = x => x.MarkupPercentage },
                //new() { Title = "Remarks/Details", Value = x => x.Remarks ?? "N/A" },
                new() {
                    Title = "Status",
                    Value = x => x.Active == true ? "Active" : "Inactive",
                    UseBadge = true,
                    BadgeClass = x => x.Active == true ? "badge-approved" : "badge-rejected"
                }
            };

            await Task.CompletedTask;
        }

        private async Task LoadGridDatasetAsync()
        {
            loadingStateActive = true;
            StateHasChanged(); // Instantly show overlay spinner block

            try
            {              
                var resultSet = (await productCategoryRepository.GetAllAsync());
                productCategoryList = resultSet != null && resultSet.Any() ? resultSet.ToList() : new List<ProductCategoryDto>();

            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged(); // Remove overlay mask automatically
            }
        }

        private async Task HandleFormExecution(long extractedRecordId)
        {
            activeRecordId = extractedRecordId;
            formTitle = extractedRecordId == 0 ? "Add Product/Service Category" : "Modify Product/Service Category";

            if (canvasShell != null)
            {
                await canvasShell.OpenAsync(formTitle);
            }
        }

        private async Task RefreshWorkspaceGridData()
        {
            if (canvasShell != null)
            {
                await canvasShell.CloseAsync();
            }
            await LoadGridDatasetAsync();
        }

        private async Task DeleteSelectedBank(ProductCategoryDto targetCategory)
        {
            var success = await productCategoryRepository!.DeleteAsync(targetCategory);
            if (success)
            {
                await LoadGridDatasetAsync();
            }
        }


        async Task ProcessExecutionFeedback(SaveResult _result)
        {
            if (_result.Success)
            {
                _Toast!.ShowSuccess(_result.Message, sessionService!.AppTitle);
            }
            else
            {
                _Toast!.ShowError(_result.Message, "Error encountered while saving");
            }

            if (_result.ClosePanel)
            {
                if (canvasShell != null) await canvasShell!.CloseAsync();
            }

            await LoadGridDatasetAsync();
            StateHasChanged();
        }


        //PDF EXPORT 
        //private async Task ExecutePrintFormatProcess(List<ProductCategory> targetedDataset)
        private async Task ExecutePrintFormatProcess(List<ProductCategoryDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;
                StateHasChanged();

                //MOVE DATA TO FORMATTTED DTO
                var PrintDataSet = targetedDataset.Select(item => new ProductCategoryDto
                {
                    ProductCategoryName = item.ProductCategoryName,
                    IncomeTypeName = item.IncomeTypeName,
                    MarkupPercentage = item.MarkupPercentage,
                    UoM = item.UoM,
                    Active = item.Active,
                    Remarks = item.Remarks
                }).ToList();


                // 1. Render the structured ledger to binary bytes array
                byte[] pdfReportBytes = await PdfService.GenerateReportDataPdfAsync(PrintDataSet, "Product Category Summary");

                // 2. Set file naming structures
                string targetFileName = $"Product_Category_Master Ledger_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                // 3. Hand off the base64 text variant stream directly down to the browser storage downloads link engine
                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", targetFileName, Convert.ToBase64String(pdfReportBytes));

                _Toast!.ShowSuccess("PDF document spooled to your downloads directory..", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"PDF Engine error: {ex.Message}", sessionService!.AppTitle);
            }
            finally
            {
                loadingStateActive = false;
                StateHasChanged();
            }
        }



        //private async Task ExecutePrintFormatProcess(List<Bank> targetedDataset)
        //{
        //    _Toast!.ShowInfo($"Preparing {targetedDataset.Count} records for system print spool...", sessionService!.AppTitle);

        //    // For elegant layout printing, you can trigger standard window print.
        //    // CSS @media print rules in your app stylesheet can strip away sidebars/nav panels automatically.
        //    await JS.InvokeVoidAsync("window.print");
        //}

        
        /// 2. Action: Export current active listing straight into a downloadable Excel Binary stream
        
        private async Task ExecuteExcelExportProcess(List<ProductCategoryDto> targetedDataset)
        {
            try
            {
                loadingStateActive = true;

                // Pass to your application reporting tool layer (e.g., using ClosedXML or EPPlus)
                byte[] excelBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Registered Banks List");

                // Use a standard JavaScript save file utility to trigger an instant download stream
                string fileName = $"Product_Category_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                await JS.InvokeVoidAsync("ZabFileSaver.DownloadBinaryStream", fileName, Convert.ToBase64String(excelBytes));

                _Toast.ShowSuccess("Excel spreadsheet compilation completed successfully.", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Excel Export Aborted: {ex.Message}", sessionService!.AppTitle);
            }
            finally
            {
                loadingStateActive = false;
            }
        }

        
        /// 3. Action: Email document attachments down to targeted distribution users
        
        private async Task ExecuteEmailDistributionProcess(List<ProductCategoryDto> targetedDataset)
        {
            try
            {
                //loadingStateActive = true;

                //// Automatically compile dataset into an excel/pdf asset attachment wrapper
                //byte[] attachmentReportBytes = await ExcelService.GenerateDataReportExcelAsync(targetedDataset, "Email Distribution Extract");

                //var emailPayload = new EmailReportRequest
                //{
                //    RecipientAddress = "zabronm@yahoo.co.za",
                //    SubjectTitle = $"System Extract: Current Active Funder Registrations ({targetedDataset.Count} Rows)",
                //    MessageBodyText = "<p>Please find attached the master data record matrix for all registered banking/funding configurations currently loaded on the system environment data space.</p>",
                //    AttachmentBytes = attachmentReportBytes,
                //    AttachmentName = "MasterData_Funder_Report.xlsx"
                //};

                //bool sendStatus = await EmailService.SendSystemReportWithAttachmentAsync(emailPayload);

                //if (sendStatus)
                //    _Toast!.ShowSuccess("List email sent successfully.");
                //else
                //    _Toast!.ShowWarning("Errors encountered while processing email.", sessionService!.AppTitle);
            }
            catch (Exception ex)
            {
                _Toast!.ShowError($"Email Transmission Engine Intercepted Error: {ex.Message}", sessionService!.AppTitle);
            }
            finally
            {
                loadingStateActive = false;
            }
        }



    }
}

