using Dapper.Contrib.Extensions;
using System.ComponentModel.DataAnnotations.Schema;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels;

[Dapper.Contrib.Extensions.Table("tblAssessmentProjectionAssumptions")]
public sealed class AssessmentProjectionAssumptions : IEntity, IAuditableEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AssessmentProjectionAssumptionsId { get; set; }

    public long AssessmentId { get; set; }
    public decimal SalesGrowthRate { get; set; }
    public int SalesGrowthStartMonth { get; set; } = 1;
    public decimal CashSalesPercentage { get; set; }
    public decimal BadDebtRate { get; set; }
    public decimal? CostOfSalesPercentage { get; set; }
    public decimal MinimumClosingStockPercentage { get; set; }
    public decimal ExpenseIncreaseRate { get; set; }
    public int ExpenseIncreaseStartMonth { get; set; } = 1;
    public decimal ContingencyRate { get; set; }
    public int VatPaymentFrequencyMonths { get; set; } = 2;
    public decimal DirectorWageIncreaseRate { get; set; }
    public decimal MinimumCashBalance { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public string? ConfirmedBy { get; set; }
    public bool Active { get; set; } = true;
    public string Remarks { get; set; } = string.Empty;
    public long CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public long ModifiedBy { get; set; }
    public DateTime ModifiedDate { get; set; }

    long IEntity.Id => AssessmentProjectionAssumptionsId;
}
