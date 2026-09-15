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
       
        /// Get recent assessments for a specific user
       
        Task<List<AssessmentModel>> GetRecentAssessmentsAsync(long userId, int count = 5);

       
        /// Get assessments by status
       
        Task<List<AssessmentModel>> GetAssessmentsByStatusAsync(long userId, string status, int count = 10);

       
        /// Get assessment by ID
       
        Task<AssessmentModel> GetAssessmentByIdAsync(long assessmentId);
    }
}