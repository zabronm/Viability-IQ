using Microsoft.AspNetCore.Components;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Finance.Implementations;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Web.Services;
using ViabilityIQ.Web.Components.CommonComponents;
using ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents;
using ViabilityIQ.Web.Components.Pages_Assessments;


namespace ViabilityIQ.Web.Components.Pages_Assessments
{
    public partial class AssessmentStockPage : ComponentBase
    {
        [Inject] private IGenericDataRepository<AssessmentStock> DataRepository { get; set; } = default!;
        [Inject] ZabOffCanvasService? zabCanvasService { get; set; }
        [Inject] MasterDataService? ViqCrudService { get; set; }
        [Inject] ISessionService? sessionService { get; set; }
        [Inject] ToastService? _Toast { get; set; }

        private List<AssessmentStockDto> StockDataList = new();
        private List<UnifiedStockViewModel> FilteredStockList = new();
        //private List<AssessmentStockDto> FilteredStockList = new();

        private bool Isloading = false;
        private string SearchTerm = string.Empty;


        protected override async Task OnInitializedAsync() => await LoadStockData();

        private async Task LoadStockData()
        {
            try
            {
                Isloading = true;
                StockDataList = (await ViqCrudService!.GetListAsync<AssessmentStockDto>("vw_assessment_stock_list",
               new { AssessmentId = sessionService!.AssessmentId }, "AssessmentId"))?.ToList() ?? new();

               FilterData();

            }
            catch (Exception ex)
            {
                _Toast!.ShowError("Error encountered: Could not load selected stock payload, please retry.", sessionService!.AppTitle);
            }
            finally
            {
                Isloading = false;
                StateHasChanged();
            }           
        }

        private void FilterData()
        {
            // Projection: Convert DTOs to the Unified ViewModel
            var sourceList = string.IsNullOrWhiteSpace(SearchTerm)
                ? StockDataList
                : StockDataList.Where(x => x.AssessmentSalesCategoryName!.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

            FilteredStockList = sourceList.Select(dto => new UnifiedStockViewModel
            {
                Id = dto.AssessmentStockId,
                AssessmentSalesCategoryName = dto.AssessmentSalesCategoryName ?? "Unknown",
                Description = dto.Description ?? "",
                blIncludeVAT = dto.blIncludeVAT,
                MonthlyValues = new decimal[] { dto.Month_1, dto.Month_2, dto.Month_3, dto.Month_4, dto.Month_5, dto.Month_6, dto.Month_7, dto.Month_8, dto.Month_9, dto.Month_10, dto.Month_11, dto.Month_12 }
            }).ToList();
        }

        // Update GetMonthValue to work with UnifiedStockViewModel
        private decimal GetMonthValue(UnifiedStockViewModel item, int month) => item.MonthlyValues[month - 1];

        // Update GetTotalForMonth to work with UnifiedStockViewModel
        private decimal GetTotalForMonth(int month) => FilteredStockList.Sum(x => x.MonthlyValues[month - 1]);


        private async Task OpenStockDataEntryPanel(long stockId)
        {
            await zabCanvasService!.ShowAsync(new CanvasRequest
            {
                Title = stockId == 0 ? "Add Monthly Stock Movement" : "Edit Monthly Stock Movement",
                ComponentType = typeof(AssessmentStockFormComponent),
                Width = 350,
                Parameters = new
                {
                    AssessmentId = sessionService!.AssessmentId,
                    AssessmentStockId = stockId,
                },
                ResultCallback = HandleStockFormUpdate,
            });
        }


        private async Task HandleStockFormUpdate(SaveResult result)
        {
            if (result.Success)
            {
                _Toast!.ShowSuccess(result.Message, sessionService!.AppTitle);
                if (result.RefreshGrid)
                    await LoadStockData();

                StateHasChanged();
            }
            else
            {
                _Toast!.ShowError(result.Message, sessionService!.AppTitle);
            }
        }
    }
}