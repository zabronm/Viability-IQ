using Dapper.Contrib.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModelsInterfaces;


/// AssetDepreciation - Track depreciation calculations over time
namespace ViabilityIQ.Shared.DataModels
{
    [Dapper.Contrib.Extensions.Table("tblAssetDepreciation")]
    public class AssetDepreciation : IEntity, IAuditableEntity, ISortableEntity
    {

        [Key] public long AssetDepreciationId { get; set; }

        [ForeignKey(nameof(AssessmentAsset))]
        public long AssessmentAssetId { get; set; }

        public long AssessmentId { get; set; }
        public DateTime DepreciationDate { get; set; }
        public decimal DepreciationAmount { get; set; }
        public decimal AccumulatedDepreciationBefore { get; set; }
        public decimal AccumulatedDepreciationAfter { get; set; }
        public string? Method { get; set; }

        public bool Active { get; set; }
        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public long CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public long ModifiedBy { get; set; }


        long IEntity.Id => AssetDepreciationId;
        string ISortableEntity.DisplayName => Method ?? string.Empty;


        // Navigation
        public virtual AssessmentAsset? AssessmentAsset { get; set; }
    }
}
