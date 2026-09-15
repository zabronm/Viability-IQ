using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Application.Interfaces
{
    public interface IAssessmentDataValidationService
    {      
        /// Check if assessment has data in specific data modules     
        Task<AssessmentDataStatus> ValidateAssessmentDataAsync(long assessmentId);
      
        /// Check if specific data type exists for an assessment      
        Task<bool> DataTypeExistsAsync(long assessmentId, string dataType);
      
        /// Get count of records for a specific data type      
        Task<int> GetDataTypeCountAsync(long assessmentId, string dataType);
    }
}
