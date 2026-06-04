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
            long assessmentId,
            string caseNumber,
            long? businessId,
            string businessName,
            long? clientId,
            string clientName,
            string assessmentType);

        void ClearAssessment();

        // ====================================================
        // NAVIGATION
        // ====================================================

        void UpdateCurrentPage(
            string pageRoute);

        // ====================================================
        // GENERAL
        // ====================================================

        void ClearWorkflow();

        void TerminateSession();
    }
}