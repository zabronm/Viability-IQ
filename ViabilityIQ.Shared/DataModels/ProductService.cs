using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels
{
    [Dapper.Contrib.Extensions.Table("tblProductService")]
    public class ProductService: IEntity, ISortableEntity, IAuditableEntity
    {
        [Key] public long ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? OtherName { get; set; }
        public string? UOM { get; set; }
        public decimal? MarkupPercentage { get; set; }  //will check if charged per product or product category
        public long ProductCategoryId { get; set; }
        public bool ProductOrService { get; set; }=true;
        public string? Remarks { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public long ModifiedBy { get; set; }

        long IEntity.Id => ProductId;
        string ISortableEntity.DisplayName => ProductName;


    }
}
