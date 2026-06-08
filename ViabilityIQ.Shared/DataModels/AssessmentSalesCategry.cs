using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblAssessmentSalesCategory")]
    public class AssessmentSalesCategory : IEntity, IAuditableEntity, ISortableEntity
    {
        [Key] public long AssessmentSalesCategoryId { get; set; }
        public long AssessmentId { get; set; }
        public string SalesCategoryName { get; set; }
        public decimal MarkupPercentage { get; set; }
        public bool Active { get; set; }
        public string Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long ModifiedBy { get; set; }

        long IEntity.Id => AssessmentSalesCategoryId;
        string ISortableEntity.DisplayName => SalesCategoryName;

    }
}
