using Microsoft.AspNetCore.Components;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.Repositories;
using ViabilityIQ.Shared.DataModels;


namespace ViabilityIQ.Web.Components.Pages_Assessments.PageFormComponents
{
    public partial class AssessmentLoansListComponent
    {

        [Inject] ISessionService? sessionService { get; set; }
        [Inject] IGenericDataRepository<AssessmentLoanDto>? AssessmentLoanRepository { get; set; }
        [Inject] MasterDataService? MasterData { get; set; }
        [Parameter] public long AssessmentId { get; set; }
        [Parameter] public EventCallback<long> OnEditRequested { get; set; }

        private IEnumerable<AssessmentLoanDto> Loans { get; set; } = Enumerable.Empty<AssessmentLoanDto>();
        private bool IsComponentLoading { get; set; } = true;
        private string DebugLogMessage { get; set; } = "Initializing lifecycle...";

        protected override async Task OnParametersSetAsync()
        {
            AssessmentId = sessionService.AssessmentId.Value;
            await LoadAssessmentLoanAsync();
        }

        public async Task RefreshAsync()
        {
            await LoadAssessmentLoanAsync();
            StateHasChanged();
        }

        private async Task LoadAssessmentLoanAsync()
        {
            try
            {
                IsComponentLoading = true;
                DebugLogMessage = "Fetching repository collections...";
                StateHasChanged();

                // FORCE the database operation onto an isolated background thread pool worker to prevent deadlocks
                var rawRecords = await Task.Run(async () =>
                {
                    return await MasterData!.GetAssessmentLoansByIdAsync(AssessmentId);
                    //return await AssessmentLoanRepository!.GetAllAsync();
                });

                if (rawRecords != null)
                {
                    // Removed "&& x.Active" filter layer so both Active and Inactive entries load into the layout matrix safely
                    Loans = rawRecords
                        .Where(x => x.AssessmentId == AssessmentId)
                        .ToList();

                    DebugLogMessage = $"Data tracking complete. Records count: {Loans.Count()}";
                }
                else
                {
                    DebugLogMessage = "Repository data returned null payload streams.";
                    Loans = Enumerable.Empty<AssessmentLoanDto>();
                }
            }
            catch (Exception ex)
            {
                DebugLogMessage = $"Exception caught in thread loop: {ex.Message}";
                Loans = Enumerable.Empty<AssessmentLoanDto>();
            }
            finally
            {
                IsComponentLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        public async Task RefreshListAsync()
        {
            await LoadAssessmentLoanAsync();
        }

        /// <summary>
        /// Evaluates numeric markup thresholds and returns explicit design presentation tokens
        /// </summary>
        private string GetMarkupBadgeClass(decimal markupPercent) => markupPercent switch
        {
            < 5.00m => "bg-danger text-white",
            >= 5.00m and < 15.00m => "bg-warning text-dark",
            >= 15.00m and < 30.00m => "bg-info text-dark",
            >= 30.00m and <= 50.00m => "bg-success text-white",
            _ => "bg-white text-dark border border-danger" // > 50 design block setup
        };

        private string GetInterestBadgeClass(decimal markupPercent) => markupPercent switch
        {
            <= 9.00m => "bg-info text-black",
            > 9.00m and < 15.00m => "bg-warning text-dark",
            >15.00m => "bg-danger text-white",           
            _ => "bg-white text-dark border border-danger" // > 50 design block setup
        };
    }
}
