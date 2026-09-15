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
    [Dapper.Contrib.Extensions.Table("tblAssetType")]
    public class AssetType : IEntity, IAuditableEntity, ISortableEntity
    {
        [Key] public long AssetTypeId { get; set; }
        [ForeignKey(nameof(AssetCategory))]
        public long AssetCategoryId { get; set; }

        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DefaultDepreciationRate { get; set; }  // Default % for this type
        public int? DefaultUsefulLifeYears { get; set; }
        public bool IsDepreciable { get; set; }
        public bool IsCurrent { get; set; }

        public bool Active { get; set; } = true;
        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => AssetTypeId;
        string ISortableEntity.DisplayName => TypeName ?? string.Empty;
    }
}
