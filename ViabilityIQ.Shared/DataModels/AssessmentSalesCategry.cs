using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;


namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblAssessmentSalesCategory")]
    public class AssessmentSalesCategory : IEntity, IAuditableEntity, ISortableEntity
    {
        [Dapper.Contrib.Extensions.Key] public long AssessmentSalesCategoryId { get; set; }
        public long AssessmentId { get; set; }

        [Required(ErrorMessage ="Sales category description is required")]
        public string? AssessmentSalesCategoryName { get; set; }

        [Required(ErrorMessage="Income type is required, please select one.")]
        public long IncomeTypeId { get; set; }

        [Range (0,1000,ErrorMessage ="Markup must be greater than zero (0)")]
        [Required(ErrorMessage ="Markup is required, please specify here.")]
        public decimal MarkupPercentage { get; set; }
        public decimal OpeningStock { get; set; }       //Opening stock will be summed to  opening stock of all products
        public bool Active { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long ModifiedBy { get; set; }

        long IEntity.Id => AssessmentSalesCategoryId;
        string ISortableEntity.DisplayName => AssessmentSalesCategoryName;

    }
}
