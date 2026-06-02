using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{
    [Table("tblProductCategory")] 
    public class ProductCategory: IEntity, ISortableEntity, IAuditableEntity
    {
        [Dapper.Contrib.Extensions.Key] public long ProductCategoryId { get; set; }
        [Required] public string? ProductCategoryName { get; set; }
        public string? UOM { get; set; }
        public decimal? MarkupPercentage { get; set; }

    
        public string? Remarks { get; set; }
        public bool Active { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => ProductCategoryId;
        string ISortableEntity.DisplayName => ProductCategoryName;

    }
}
