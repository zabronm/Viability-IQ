using ViabilityIQ.Application.Interfaces;

namespace ViabilityIQ.Web.Services
{
    public class SessionService : ISessionService
    {

        // ====================================================
        // USER CONTEXT
        // ====================================================  
        private string _appTitle = "Viability.IQ";


        // ====================================================
        // USER CONTEXT
        // ====================================================       

        private long _userId = -99;
        private string _userName = string.Empty;
        private string _userEmail = string.Empty;
        private bool _isAuthenticated;

        // ====================================================
        // BUSINESS CONTEXT
        // ====================================================

        private long? _businessId;
        private string _businessName = string.Empty;

        // ====================================================
        // ASSESSMENT CONTEXT
        // ====================================================

        private long? _assessmentId;
        private string? _assessmentNumber;

        private long? _phaseId;
        private string _phaseName = string.Empty;

        // ====================================================
        // CLIENT CONTEXT
        // ====================================================

        private long? _clientId;
        private string _clientName = string.Empty;

        // ====================================================
        // NAVIGATION
        // ====================================================

        private string _currentPage = string.Empty;

        // ====================================================
        // EVENTS
        // ====================================================

        public event Action? OnSessionChanged;

        // ====================================================
        // PUBLIC READ-ONLY PROPERTIES
        // ====================================================

        public string AppTitle => _appTitle;
        public long UserId => _userId;
        public string UserName => _userName;
        public string UserEmail => _userEmail;
        public bool IsAuthenticated => _isAuthenticated;
        public long? BusinessId => _businessId;
        public string BusinessName => _businessName;
        public long? AssessmentId => _assessmentId;
        public string? AssessmentNumber => _assessmentNumber;
        public long? PhaseId => _phaseId;
        public string PhaseName => _phaseName;
        public long? ClientId => _clientId;
        public string ClientName => _clientName;
        public string CurrentPage => _currentPage;

        string ISessionService.AppTitle { get => AppTitle; set => throw new NotImplementedException(); }

        // ====================================================
        // USER
        // ====================================================

        public void EstablishUserSession(long userId, string userName, string? userEmail = null)
        {
            _userId = userId;
            _userName = userName;
            _userEmail = userEmail ?? userName;
            _isAuthenticated = true;
            _appTitle = AppTitle;

            NotifyStateChanged();
        }

        // ====================================================
        // BUSINESS
        // ====================================================

        public void SetActiveBusiness(long businessId, string businessName)
        {
            if (businessId <= 0)
                throw new ArgumentException("BusinessId must be greater than zero.");

            if (_businessId != businessId)
            {
                _businessId = businessId;
                _businessName = businessName ?? string.Empty;

                NotifyStateChanged();
            }
        }

        // ====================================================
        // ASSESSMENT
        // ====================================================

        public void SetActiveAssessment(long assessmentId, string? assessmentNumber = null, long? phaseId = null, string phaseName = "")
        {
            if (assessmentId <= 0)
                throw new ArgumentException("AssessmentId must be greater than zero.");

            if (_assessmentId != assessmentId || _phaseId != phaseId)
            {
                _assessmentId = assessmentId;
                _assessmentNumber = assessmentNumber;
                _phaseId = phaseId;
                _phaseName = phaseName;

                NotifyStateChanged();
            }
        }

        // ====================================================
        // CLIENT
        // ====================================================

        public void SetActiveClient(long clientId, string clientName)
        {
            if (clientId <= 0)
                throw new ArgumentException("ClientId must be greater than zero.");

            if (_clientId != clientId)
            {
                _clientId = clientId;
                _clientName = clientName ?? string.Empty;
                NotifyStateChanged();
            }
        }

        // ====================================================
        // NAVIGATION
        // ====================================================

        public void UpdateCurrentPage(
            string pageRoute)
        {
            if (_currentPage != pageRoute)
            {
                _currentPage = pageRoute ?? string.Empty;
                NotifyStateChanged();
            }
        }

        // ====================================================
        // CLEAR METHODS
        // ====================================================

        public void ClearAssessment()
        {
            _assessmentId = null;
            _assessmentNumber = null;

            _phaseId = null;
            _phaseName = string.Empty;
            NotifyStateChanged();
        }

        public void ClearBusiness()
        {
            _businessId = null;
            _businessName = string.Empty;
            ClearAssessment();
            NotifyStateChanged();
        }

        public void ClearClient()
        {
            _clientId = null;
            _clientName = string.Empty;
            NotifyStateChanged();
        }

        public void ClearWorkflow()
        {
            ClearBusiness();
            ClearClient();
            NotifyStateChanged();
        }

        // ====================================================
        // LOGOUT
        // ====================================================

        public void TerminateSession()
        {
            ClearWorkflow();
            _userId = 0;
            _userName = string.Empty;
            _userEmail = string.Empty;
            _isAuthenticated = false;
            _currentPage = string.Empty;
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
