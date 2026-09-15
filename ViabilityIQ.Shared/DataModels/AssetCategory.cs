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
    [Table("tblAssetCategory")]
    public class AssetCategory: IEntity, IAuditableEntity, ISortableEntity
    {
        [Dapper.Contrib.Extensions.Key] public long AssetCategoryId { get; set; }

        [Required(ErrorMessage = "Asset Category Name is required.")]
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsCurrentAsset { get; set; }  // TRUE for current, FALSE for non-current
        public string? BalanceSheetSection { get; set; }  // For balance sheet grouping

        public bool Active { get; set; } = true;
        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }

        long IEntity.Id => AssetCategoryId;
        string ISortableEntity.DisplayName => CategoryName;
    }
}
