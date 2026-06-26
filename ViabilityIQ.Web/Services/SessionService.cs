using System;
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

        private bool _hasSalesData;
        private bool _hasStockData;
        private bool _hasExpensesData;
        private bool _hasReportsData;
        private bool _hasReviewsData;
        private bool _hasReviews;
        private bool _hasSalesEntries;
        private bool _hasStockEntries;
        private bool _hasExpensesEntries;
        private bool _hasReportsEntries;
        private bool _hasReviewsEntries;
        private bool _hasReportsGenerated;

        private long? _businessId;
        private string _businessName = string.Empty;

        private long? _clientId;
        private string _clientName = string.Empty;
        private string _assessmentType = string.Empty;

        public long? AssessmentId => _assessmentId;
        public string CaseNumber => _caseNumber;

        public bool HasSalesData => _hasSalesData;
        public bool HasStockData => _hasStockData;
        public bool HasExpensesData => _hasExpensesData;
        public bool HasReportsData => _hasReportsData;
        public bool HasReviewsData => _hasReviewsData;
        public bool HasReviews => _hasReviews;
        public bool HasSalesEntries => _hasSalesEntries;
        public bool HasStockEntries => _hasStockEntries;
        public bool HasExpensesEntries => _hasExpensesEntries;
        public bool HasReportsEntries => _hasReportsEntries;
        public bool HasReviewsEntries => _hasReviewsEntries;
        public bool HasReportsGenerated => _hasReportsGenerated;

        // Computed logic check matching the contract spec rules
        public bool HasAnyEntries => _hasSalesEntries || _hasStockEntries || _hasExpensesEntries || _hasReportsEntries || _hasReviewsEntries;

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
            bool hasSalesData,
            bool hasStockData,
            bool hasExpensesData,
            bool hasReportsData,
            bool hasReviewsData,
            bool hasReviews,
            string assessmentType)
        {
            _assessmentId = assessmentId;
            _caseNumber = caseNumber ?? string.Empty;

            _businessId = businessId;
            _businessName = businessName ?? string.Empty;

            _clientId = clientId;
            _clientName = clientName ?? string.Empty;
            _assessmentType = assessmentType ?? string.Empty;

            // Map data contextual indicator inputs cleanly
            _hasSalesData = hasSalesData;
            _hasStockData = hasStockData;
            _hasExpensesData = hasExpensesData;
            _hasReportsData = hasReportsData;
            _hasReviewsData = hasReviewsData;
            _hasReviews = hasReviews;

            // Deriving entries or evaluations relative to active indicators
            _hasSalesEntries = hasSalesData;
            _hasStockEntries = hasStockData;
            _hasExpensesEntries = hasExpensesData;
            _hasReportsEntries = hasReportsData;
            _hasReviewsEntries = hasReviewsData;
            _hasReportsGenerated = hasReportsData;

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
            _assessmentType = string.Empty;

            // Reset indicators safely
            _hasSalesData = false;
            _hasStockData = false;
            _hasExpensesData = false;
            _hasReportsData = false;
            _hasReviewsData = false;
            _hasReviews = false;
            _hasSalesEntries = false;
            _hasStockEntries = false;
            _hasExpensesEntries = false;
            _hasReportsEntries = false;
            _hasReviewsEntries = false;
            _hasReportsGenerated = false;

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