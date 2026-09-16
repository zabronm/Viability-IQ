using System.ComponentModel.DataAnnotations;
using Dapper.Contrib.Extensions;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels;

[Table("tblAssessmentVATTransactions")]
public sealed class AssessmentVATTransactions : IEntity, IAuditableEntity, ISortableEntity
{
    [Dapper.Contrib.Extensions.Key]
    public long VATTransactionId { get; set; }

    [Required]
    public long AssessmentId { get; set; }

    public long SourceId { get; set; }

    [Required, StringLength(50)]
    public string SourceType { get; set; } = "Adjustment";

    [Range(1, 12)]
    public int Period { get; set; }

    [Range(0, 100)]
    public decimal VATRate { get; set; }

    public decimal TaxableAmount { get; set; }
    public decimal OutputVAT { get; set; }
    public decimal InputVAT { get; set; }
    public decimal VATPayable { get; set; }
    public bool Active { get; set; } = true;

    [StringLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public long ModifiedBy { get; set; }

    [Write(false)]
    public string DisplayName => $"M{Period} {SourceType}";

    long IEntity.Id => VATTransactionId;
    string ISortableEntity.DisplayName => DisplayName;

    public void RecalculatePayable()
    {
        VATPayable = OutputVAT - InputVAT;
    }
}
