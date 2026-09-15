using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface ISessionService
    {
        event Action? OnSessionChanged;

        // ====================================================
        // APPLICATION
        // ====================================================
        string AppTitle { get; set; }

        // ====================================================
        // USER CONTEXT
        // ====================================================
        long UserId { get; }
        string UserName { get; }
        string UserEmail { get; }
        bool IsAuthenticated { get; }

        long CompanyId { get; }
        long BranchId { get; }
        long ProvinceId { get; }

        // ====================================================
        // ASSESSMENT CONTEXT
        // ====================================================
        long? AssessmentId { get; }
        string CaseNumber { get; }
        bool HasAssetsData { get; }
        bool HasSalesData { get; }
        bool HasStockData { get; }
        bool HasExpensesData { get; }
        bool HasReportsData { get; }
        bool HasReviewsData { get; }
        bool HasReviews { get; }
        bool HasDebtorsCreditorsData { get; }
        bool HasLoansData { get; }

        bool HasAssetsEntries { get; }
        bool HasSalesEntries { get; }
        bool HasStockEntries { get; }
        bool HasExpensesEntries { get; }
        bool HasReportsEntries { get; }
        bool HasReviewsEntries { get; }
        bool HasAnyEntries { get; }
        bool HasReportsGenerated { get; }

        long? BusinessId { get; }
        string BusinessName { get; }
        long? ClientId { get; }
        string ClientName { get; }
        string AssessmentType { get; }

        // ====================================================
        // NAVIGATION
        // ====================================================
        string CurrentPage { get; }

        // ====================================================
        // LOGIN
        // ====================================================
        void EstablishUserSession(
            long userId,
            string userName,
            string userEmail,
            long companyId,
            long branchId,
            long provinceId);

        // ====================================================
        // ASSESSMENT
        // ====================================================
        void SetActiveAssessment(
            string caseNumber,
            long assessmentId,
            long businessId,
            string businessName,
            long clientId,
            string clientName,
            string assessmentType,
            bool HasAssetsData,
            bool HasExpensesData,
            bool HasSalesData,
            bool HasStockData,
            bool HasReportsData,
            bool HasReviewsData,
            bool HasReviews,
            bool HasDebtorsCreditorsData,
            bool HasLoansData);


        void ClearAssessment();

        // ====================================================
        // NAVIGATION
        // ====================================================
        void UpdateCurrentPage(string pageRoute);

        // ====================================================
        // GENERAL
        // ====================================================
        void ClearWorkflow();
        void TerminateSession();



        // ====================================================
        // DATA VALIDATION ENGINE (NEW)
        // ====================================================
        AssessmentDataStatus GetAssessmentDataStatus();
        bool HasAnyAssessmentData();
        List<string> GetMissingAssessmentData();
        Dictionary<string, bool> GetAllAssessmentDataStatus();
    }
}
