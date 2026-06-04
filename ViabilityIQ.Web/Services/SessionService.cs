using ViabilityIQ.Application.Interfaces;

namespace ViabilityIQ.Web.Services
{
    public class SessionService : ISessionService
    {
        // ====================================================
        // APPLICATION
        // ====================================================

        private string _appTitle = "Viability.IQ";

        public string AppTitle
        {
            get => _appTitle;
            set
            {
                _appTitle = value;
                NotifyStateChanged();
            }
        }

        // ====================================================
        // USER CONTEXT
        // ====================================================

        private long _userId = -99;
        private string _userName = string.Empty;
        private string _userEmail = string.Empty;
        private bool _isAuthenticated;

        private long _companyId;
        private long _branchId;
        private long _provinceId;

        public long UserId => _userId;
        public string UserName => _userName;
        public string UserEmail => _userEmail;
        public bool IsAuthenticated => _isAuthenticated;

        public long CompanyId => _companyId;
        public long BranchId => _branchId;
        public long ProvinceId => _provinceId;

        // ====================================================
        // ASSESSMENT CONTEXT
        // ====================================================

        private long? _assessmentId;
        private string _caseNumber = string.Empty;

        private long? _businessId;
        private string _businessName = string.Empty;

        private long? _clientId;
        private string _clientName = string.Empty;
        private string _assessmentType = string.Empty;


        public long? AssessmentId => _assessmentId;
        public string CaseNumber => _caseNumber;

        public long? BusinessId => _businessId;
        public string BusinessName => _businessName;

        public long? ClientId => _clientId;
        public string ClientName => _clientName;
        public string AssessmentType => _assessmentType;

        // ====================================================
        // NAVIGATION
        // ====================================================

        private string _currentPage = string.Empty;

        public string CurrentPage => _currentPage;

        // ====================================================
        // EVENTS
        // ====================================================

        public event Action? OnSessionChanged;

        // ====================================================
        // LOGIN
        // ====================================================

        public void EstablishUserSession(
            long userId,
            string userName,
            string userEmail,
            long companyId,
            long branchId,
            long provinceId)
        {
            _userId = userId;
            _userName = userName;
            _userEmail = userEmail;

            _companyId = companyId;
            _branchId = branchId;
            _provinceId = provinceId;

            _isAuthenticated = true;

            NotifyStateChanged();
        }

        // ====================================================
        // ASSESSMENT
        // ====================================================

        public void SetActiveAssessment(
            long assessmentId,
            string caseNumber,
            long? businessId,
            string businessName,
            long? clientId,
            string clientName,
            string assessmentType)
        {
            _assessmentId = assessmentId;
            _caseNumber = caseNumber ?? string.Empty;

            _businessId = businessId;
            _businessName = businessName ?? string.Empty;

            _clientId = clientId;
            _clientName = clientName ?? string.Empty;
            _assessmentType = assessmentType ?? string.Empty;

            NotifyStateChanged();
        }

        public void ClearAssessment()
        {
            _assessmentId = null;
            _caseNumber = string.Empty;

            _businessId = null;
            _businessName = string.Empty;

            _clientId = null;
            _clientName = string.Empty;

            NotifyStateChanged();
        }

        // ====================================================
        // NAVIGATION
        // ====================================================

        public void UpdateCurrentPage(string pageRoute)
        {
            _currentPage = pageRoute ?? string.Empty;

            NotifyStateChanged();
        }

        // ====================================================
        // GENERAL
        // ====================================================

        public void ClearWorkflow()
        {
            ClearAssessment();
        }

        // ====================================================
        // LOGOUT
        // ====================================================

        public void TerminateSession()
        {
            _userId = -99;
            _userName = string.Empty;
            _userEmail = string.Empty;

            _companyId = 0;
            _branchId = 0;
            _provinceId = 0;

            _isAuthenticated = false;

            _currentPage = string.Empty;

            ClearAssessment();

            NotifyStateChanged();
        }

        // ====================================================
        // INTERNAL
        // ====================================================

        private void NotifyStateChanged()
        {
            OnSessionChanged?.Invoke();
        }
    }
}