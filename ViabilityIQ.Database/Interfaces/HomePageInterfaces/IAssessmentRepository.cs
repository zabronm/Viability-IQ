using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels.HomePageModels;

namespace ViabilityIQ.Application.Interfaces.HomePageInterfaces
{
  
    /// Repository interface for assessment data access (dashboard)
   
    public interface IAssessmentRepository
    {
        /// <summary>
        /// Get recent assessments for a specific user
        /// </summary>
        Task<List<AssessmentModel>> GetRecentAssessmentsAsync(long userId, int count = 5);

        /// <summary>
        /// Get assessments by status
        /// </summary>
        Task<List<AssessmentModel>> GetAssessmentsByStatusAsync(long userId, string status, int count = 10);

        /// <summary>
        /// Get assessment by ID
        /// </summary>
        Task<AssessmentModel> GetAssessmentByIdAsync(long assessmentId);
    }
}