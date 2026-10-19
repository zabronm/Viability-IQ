using Dapper.Contrib.Extensions;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels;

[Table("tblAssessmentManufacturingBalances")]
public sealed class AssessmentManufacturingBalances : IEntity, IAuditableEntity
{
    [Key] public long AssessmentManufacturingBalancesId { get; set; }
    public long AssessmentId { get; set; }
    public long? AssessmentSalesCategoryId { get; set; }
    public decimal OpeningWorkInProgress { get; set; }
    public decimal OpeningFinishedGoods { get; set; }
    public bool InventoryEstimatesConfirmed { get; set; }
    public bool Active { get; set; } = true;
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public long ModifiedBy { get; set; }

    public decimal ClosingWorkInProgress_Month_1 { get; set; }
    public decimal ClosingWorkInProgress_Month_2 { get; set; }
    public decimal ClosingWorkInProgress_Month_3 { get; set; }
    public decimal ClosingWorkInProgress_Month_4 { get; set; }
    public decimal ClosingWorkInProgress_Month_5 { get; set; }
    public decimal ClosingWorkInProgress_Month_6 { get; set; }
    public decimal ClosingWorkInProgress_Month_7 { get; set; }
    public decimal ClosingWorkInProgress_Month_8 { get; set; }
    public decimal ClosingWorkInProgress_Month_9 { get; set; }
    public decimal ClosingWorkInProgress_Month_10 { get; set; }
    public decimal ClosingWorkInProgress_Month_11 { get; set; }
    public decimal ClosingWorkInProgress_Month_12 { get; set; }

    public decimal ClosingFinishedGoods_Month_1 { get; set; }
    public decimal ClosingFinishedGoods_Month_2 { get; set; }
    public decimal ClosingFinishedGoods_Month_3 { get; set; }
    public decimal ClosingFinishedGoods_Month_4 { get; set; }
    public decimal ClosingFinishedGoods_Month_5 { get; set; }
    public decimal ClosingFinishedGoods_Month_6 { get; set; }
    public decimal ClosingFinishedGoods_Month_7 { get; set; }
    public decimal ClosingFinishedGoods_Month_8 { get; set; }
    public decimal ClosingFinishedGoods_Month_9 { get; set; }
    public decimal ClosingFinishedGoods_Month_10 { get; set; }
    public decimal ClosingFinishedGoods_Month_11 { get; set; }
    public decimal ClosingFinishedGoods_Month_12 { get; set; }

    [Write(false)]
    public decimal[] MonthlyClosingWorkInProgress
    {
        get => [ClosingWorkInProgress_Month_1, ClosingWorkInProgress_Month_2,
            ClosingWorkInProgress_Month_3, ClosingWorkInProgress_Month_4,
            ClosingWorkInProgress_Month_5, ClosingWorkInProgress_Month_6,
            ClosingWorkInProgress_Month_7, ClosingWorkInProgress_Month_8,
            ClosingWorkInProgress_Month_9, ClosingWorkInProgress_Month_10,
            ClosingWorkInProgress_Month_11, ClosingWorkInProgress_Month_12];
        set => AssignMonthly(value, (i, v) =>
        {
            switch (i)
            {
                case 0: ClosingWorkInProgress_Month_1 = v; break;
                case 1: ClosingWorkInProgress_Month_2 = v; break;
                case 2: ClosingWorkInProgress_Month_3 = v; break;
                case 3: ClosingWorkInProgress_Month_4 = v; break;
                case 4: ClosingWorkInProgress_Month_5 = v; break;
                case 5: ClosingWorkInProgress_Month_6 = v; break;
                case 6: ClosingWorkInProgress_Month_7 = v; break;
                case 7: ClosingWorkInProgress_Month_8 = v; break;
                case 8: ClosingWorkInProgress_Month_9 = v; break;
                case 9: ClosingWorkInProgress_Month_10 = v; break;
                case 10: ClosingWorkInProgress_Month_11 = v; break;
                case 11: ClosingWorkInProgress_Month_12 = v; break;
            }
        });
    }

    [Write(false)]
    public decimal[] MonthlyClosingFinishedGoods
    {
        get => [ClosingFinishedGoods_Month_1, ClosingFinishedGoods_Month_2,
            ClosingFinishedGoods_Month_3, ClosingFinishedGoods_Month_4,
            ClosingFinishedGoods_Month_5, ClosingFinishedGoods_Month_6,
            ClosingFinishedGoods_Month_7, ClosingFinishedGoods_Month_8,
            ClosingFinishedGoods_Month_9, ClosingFinishedGoods_Month_10,
            ClosingFinishedGoods_Month_11, ClosingFinishedGoods_Month_12];
        set => AssignMonthly(value, (i, v) =>
        {
            switch (i)
            {
                case 0: ClosingFinishedGoods_Month_1 = v; break;
                case 1: ClosingFinishedGoods_Month_2 = v; break;
                case 2: ClosingFinishedGoods_Month_3 = v; break;
                case 3: ClosingFinishedGoods_Month_4 = v; break;
                case 4: ClosingFinishedGoods_Month_5 = v; break;
                case 5: ClosingFinishedGoods_Month_6 = v; break;
                case 6: ClosingFinishedGoods_Month_7 = v; break;
                case 7: ClosingFinishedGoods_Month_8 = v; break;
                case 8: ClosingFinishedGoods_Month_9 = v; break;
                case 9: ClosingFinishedGoods_Month_10 = v; break;
                case 10: ClosingFinishedGoods_Month_11 = v; break;
                case 11: ClosingFinishedGoods_Month_12 = v; break;
            }
        });
    }

    long IEntity.Id => AssessmentManufacturingBalancesId;

    private static void AssignMonthly(decimal[]? values, Action<int, decimal> assign)
    {
        if (values is null || values.Length != 12)
            throw new ArgumentException("Monthly balances must have exactly 12 elements.");

        for (var index = 0; index < values.Length; index++)
            assign(index, values[index]);
    }
}
