namespace ViabilityIQ.Application.Interfaces
{
    public interface ISessionService
    {
        event Action? OnSessionChanged;

        // ====================================================\r
        // APPLICATION\r
        // ====================================================\r
        string AppTitle { get; set; }

        // ====================================================\r
        // USER CONTEXT\r
        // ====================================================\r
        long UserId { get; }
        string UserName { get; }
        string UserEmail { get; }
        bool IsAuthenticated { get; }

        long CompanyId { get; }
        long BranchId { get; }
        long ProvinceId { get; }

        // ====================================================\r
        // ASSESSMENT CONTEXT\r
        // ====================================================\r
        long? AssessmentId { get; }
        string CaseNumber { get; }
        bool HasSalesData { get; }
        bool HasStockData { get; }
        bool HasExpensesData { get; }
        bool HasReportsData { get; }
        bool HasReviewsData { get; }
        bool HasDebtorsCreditorsData { get; }
        bool HasLoansData { get; }

        bool HasReviews { get; }
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

        // ====================================================\r
        // NAVIGATION\r
        // ====================================================\r
        string CurrentPage { get; }

        // ====================================================\r
        // LOGIN\r
        // ====================================================\r
        void EstablishUserSession(
            long userId,
            string userName,
            string userEmail,
            long companyId,
            long branchId,
            long provinceId);

        // ====================================================\r
        // ASSESSMENT\r
        // ====================================================\r
        void SetActiveAssessment(
            long assessmentId,
            string caseNumber,
            long? businessId,
            string businessName,
            long? clientId,
            string clientName,
            bool HasSalesData,
            bool HasStockData,
            bool HasExpensesData,
            bool HasReportsData,
            bool HasReviewsData,
            bool HasDebtorsCreditorsData,
            bool HasLoansData,
            bool HasReviews,
            string assessmentType);

        void ClearAssessment();

        // ====================================================\r
        // NAVIGATION\r
        // ====================================================\r
        void UpdateCurrentPage(string pageRoute);

        // ====================================================\r
        // GENERAL\r
        // ====================================================\r
        void ClearWorkflow();
        void TerminateSession();
    }
}