using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.SharedModels;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Infrastructure.Repositories
{
    public class AssessmentDataValidationService: IAssessmentDataValidationService
    {
        private readonly MasterDataService _masterDataService;
        private readonly ITenantAuthorizationService _tenantAuthorizationService;
        private readonly IGenericDataRepository<Assessment> _assessmentRepository;


        // ====================================================
        // ✅ STEP 1: Define the mapping dictionary HERE
        // ====================================================
        private readonly Dictionary<string, string> _dataTypeTableMapping = new()
        {
            { "Assets", "tblAssessmentAssets" },
            { "Expenses", "tblAssessmentExpenses" },
            { "Sales", "tblAssessmentSales" },
            { "Stock", "tblAssessmentStock" },
            { "Reports", "tblSystemReports" },
            { "Reviews", "tblAssessmentReviews" },
            { "DebtorsCreditors", "tblAssessmentDebtorsCreditorsProfile" },
            { "Loans", "tblAssessmentLoan" }
        };


        // ====================================================
        // Constructor
        // ====================================================
        public AssessmentDataValidationService(
            MasterDataService masterDataService,
            ITenantAuthorizationService tenantAuthorizationService,
            IGenericDataRepository<Assessment> assessmentRepository)
        {
            _masterDataService = masterDataService ?? throw new ArgumentNullException(nameof(masterDataService));
            _tenantAuthorizationService = tenantAuthorizationService
                ?? throw new ArgumentNullException(nameof(tenantAuthorizationService));
            _assessmentRepository = assessmentRepository
                ?? throw new ArgumentNullException(nameof(assessmentRepository));
        }


        // ====================================================
        // ✅ STEP 2: Use the mapping HERE
        // ====================================================
        public async Task<int> GetDataTypeCountAsync(long assessmentId, string dataType)
        {
            await _tenantAuthorizationService.EnsureCanAccessAssessmentAsync(
                assessmentId, TenantRecordAccess.Read);
            // This method USES the dictionary defined above
            string tableName = MapDataTypeToTableName(dataType);  // ← Calls the method below

            try
            {
                int count = await _masterDataService.CountAsync(
                tableName: tableName,  // ← Uses the result
                keyField: "AssessmentId",
                keyValue: assessmentId
            );

                return count;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        // ====================================================
        // ✅ STEP 3: The method that USES the dictionary
        // ====================================================
        public string MapDataTypeToTableName(string dataType)
        {
            // This method looks up the dataType in the dictionary
            if (_dataTypeTableMapping.TryGetValue(dataType, out var tableName))
                return tableName;

            throw new ArgumentException(
                $"Unknown data type: '{dataType}'",
                nameof(dataType));
        }



        public async Task<AssessmentDataStatus> ValidateAssessmentDataAsync(long assessmentId)
        {
            var assessment = await _assessmentRepository.GetByIdAsync(assessmentId)
                ?? throw new InvalidOperationException($"Assessment {assessmentId} was not found.");
            var hasStock = assessment.AssessmentModeId == (int)AssessmentMode.Manufacturing
                ? await _masterDataService.CountAsync(
                    "tblAssessmentManufacturingMaterials",
                    "AssessmentId",
                    assessmentId) > 0
                  || await _masterDataService.CountAsync(
                    "tblAssessmentManufacturingBalances",
                    "AssessmentId",
                    assessmentId) > 0
                : await DataTypeExistsAsync(assessmentId, "Stock");

            return new AssessmentDataStatus
            {
                AssessmentId = assessmentId,
                HasAssets = await DataTypeExistsAsync(assessmentId, "Assets"),
                HasExpenses = await DataTypeExistsAsync(assessmentId, "Expenses"),
                HasSales = await DataTypeExistsAsync(assessmentId, "Sales"),
                HasStock = hasStock,
                HasReports = await DataTypeExistsAsync(assessmentId, "Reports"),
                HasReviews = await DataTypeExistsAsync(assessmentId, "Reviews"),
                HasDebtorsCreditors = await DataTypeExistsAsync(assessmentId, "DebtorsCreditors"),
                HasLoans = await DataTypeExistsAsync(assessmentId, "Loans")
            };
        }


        public async Task<bool> DataTypeExistsAsync(long assessmentId, string dataType)
        {
            int count = await GetDataTypeCountAsync(assessmentId, dataType);
            return count > 0;
        }


        
    }
}
