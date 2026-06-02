using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViabilityIQ.Application.Interfaces
{
    public interface ISessionService
    {
        event Action? OnSessionChanged;

        // User Context
        string AppTitle { get; set; }
        long UserId { get; }
        string UserName { get; }
        string UserEmail { get; }
        bool IsAuthenticated { get; }

        // Business Context
        long? BusinessId { get; }
        string BusinessName { get; }

        // Assessment Context
        long? AssessmentId { get; }
        string? AssessmentNumber { get; }
        long? PhaseId { get; }
        string PhaseName { get; }

        // Client Context
        long? ClientId { get; }
        string ClientName { get; }

        // Navigation
        string CurrentPage { get; }

        void EstablishUserSession(long userId, string userName, string? userEmail = null);

        void SetActiveBusiness(long businessId, string businessName);

        void SetActiveAssessment(long assessmentId, string? assessmentNumber = null, long? phaseId = null, string phaseName = "");

        void SetActiveClient(long clientId, string clientName);

        void UpdateCurrentPage(string pageRoute);

        void ClearAssessment();

        void ClearBusiness();

        void ClearClient();

        void ClearWorkflow();

        void TerminateSession();
    }
}
