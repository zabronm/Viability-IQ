using Dapper.Contrib.Extensions;
using ViabilityIQ.Shared.DataModelsInterfaces;

namespace ViabilityIQ.Shared.DataModels;

[Table("tblAssessmentManufacturingMaterials")]
public sealed class AssessmentManufacturingMaterial : IEntity, IAuditableEntity, ISortableEntity
{
    [Key] public long AssessmentManufacturingMaterialId { get; set; }
    public long AssessmentId { get; set; }
    public long? AssessmentSalesCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal OpeningQuantity { get; set; }
    public decimal OpeningValue { get; set; }
    public bool IncludeVAT { get; set; }
    public decimal VATRate { get; set; }
    public decimal TotalNoVAT { get; set; }
    public decimal TotalWithVAT { get; set; }
    public bool Active { get; set; } = true;
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public long ModifiedBy { get; set; }

    public decimal PurchaseQuantity_Month_1 { get; set; }
    public decimal PurchaseQuantity_Month_2 { get; set; }
    public decimal PurchaseQuantity_Month_3 { get; set; }
    public decimal PurchaseQuantity_Month_4 { get; set; }
    public decimal PurchaseQuantity_Month_5 { get; set; }
    public decimal PurchaseQuantity_Month_6 { get; set; }
    public decimal PurchaseQuantity_Month_7 { get; set; }
    public decimal PurchaseQuantity_Month_8 { get; set; }
    public decimal PurchaseQuantity_Month_9 { get; set; }
    public decimal PurchaseQuantity_Month_10 { get; set; }
    public decimal PurchaseQuantity_Month_11 { get; set; }
    public decimal PurchaseQuantity_Month_12 { get; set; }

    public decimal PurchaseValue_Month_1 { get; set; }
    public decimal PurchaseValue_Month_2 { get; set; }
    public decimal PurchaseValue_Month_3 { get; set; }
    public decimal PurchaseValue_Month_4 { get; set; }
    public decimal PurchaseValue_Month_5 { get; set; }
    public decimal PurchaseValue_Month_6 { get; set; }
    public decimal PurchaseValue_Month_7 { get; set; }
    public decimal PurchaseValue_Month_8 { get; set; }
    public decimal PurchaseValue_Month_9 { get; set; }
    public decimal PurchaseValue_Month_10 { get; set; }
    public decimal PurchaseValue_Month_11 { get; set; }
    public decimal PurchaseValue_Month_12 { get; set; }

    public decimal ClosingQuantity_Month_1 { get; set; }
    public decimal ClosingQuantity_Month_2 { get; set; }
    public decimal ClosingQuantity_Month_3 { get; set; }
    public decimal ClosingQuantity_Month_4 { get; set; }
    public decimal ClosingQuantity_Month_5 { get; set; }
    public decimal ClosingQuantity_Month_6 { get; set; }
    public decimal ClosingQuantity_Month_7 { get; set; }
    public decimal ClosingQuantity_Month_8 { get; set; }
    public decimal ClosingQuantity_Month_9 { get; set; }
    public decimal ClosingQuantity_Month_10 { get; set; }
    public decimal ClosingQuantity_Month_11 { get; set; }
    public decimal ClosingQuantity_Month_12 { get; set; }

    [Write(false)]
    public decimal[] MonthlyPurchaseQuantities
    {
        get => [PurchaseQuantity_Month_1, PurchaseQuantity_Month_2, PurchaseQuantity_Month_3,
            PurchaseQuantity_Month_4, PurchaseQuantity_Month_5, PurchaseQuantity_Month_6,
            PurchaseQuantity_Month_7, PurchaseQuantity_Month_8, PurchaseQuantity_Month_9,
            PurchaseQuantity_Month_10, PurchaseQuantity_Month_11, PurchaseQuantity_Month_12];
        set => AssignMonthly(value, (i, v) =>
        {
            switch (i)
            {
                case 0: PurchaseQuantity_Month_1 = v; break;
                case 1: PurchaseQuantity_Month_2 = v; break;
                case 2: PurchaseQuantity_Month_3 = v; break;
                case 3: PurchaseQuantity_Month_4 = v; break;
                case 4: PurchaseQuantity_Month_5 = v; break;
                case 5: PurchaseQuantity_Month_6 = v; break;
                case 6: PurchaseQuantity_Month_7 = v; break;
                case 7: PurchaseQuantity_Month_8 = v; break;
                case 8: PurchaseQuantity_Month_9 = v; break;
                case 9: PurchaseQuantity_Month_10 = v; break;
                case 10: PurchaseQuantity_Month_11 = v; break;
                case 11: PurchaseQuantity_Month_12 = v; break;
            }
        });
    }

    [Write(false)]
    public decimal[] MonthlyPurchaseValues
    {
        get => [PurchaseValue_Month_1, PurchaseValue_Month_2, PurchaseValue_Month_3,
            PurchaseValue_Month_4, PurchaseValue_Month_5, PurchaseValue_Month_6,
            PurchaseValue_Month_7, PurchaseValue_Month_8, PurchaseValue_Month_9,
            PurchaseValue_Month_10, PurchaseValue_Month_11, PurchaseValue_Month_12];
        set => AssignMonthly(value, (i, v) =>
        {
            switch (i)
            {
                case 0: PurchaseValue_Month_1 = v; break;
                case 1: PurchaseValue_Month_2 = v; break;
                case 2: PurchaseValue_Month_3 = v; break;
                case 3: PurchaseValue_Month_4 = v; break;
                case 4: PurchaseValue_Month_5 = v; break;
                case 5: PurchaseValue_Month_6 = v; break;
                case 6: PurchaseValue_Month_7 = v; break;
                case 7: PurchaseValue_Month_8 = v; break;
                case 8: PurchaseValue_Month_9 = v; break;
                case 9: PurchaseValue_Month_10 = v; break;
                case 10: PurchaseValue_Month_11 = v; break;
                case 11: PurchaseValue_Month_12 = v; break;
            }
        });
    }

    [Write(false)]
    public decimal[] MonthlyClosingQuantities
    {
        get => [ClosingQuantity_Month_1, ClosingQuantity_Month_2, ClosingQuantity_Month_3,
            ClosingQuantity_Month_4, ClosingQuantity_Month_5, ClosingQuantity_Month_6,
            ClosingQuantity_Month_7, ClosingQuantity_Month_8, ClosingQuantity_Month_9,
            ClosingQuantity_Month_10, ClosingQuantity_Month_11, ClosingQuantity_Month_12];
        set => AssignMonthly(value, (i, v) =>
        {
            switch (i)
            {
                case 0: ClosingQuantity_Month_1 = v; break;
                case 1: ClosingQuantity_Month_2 = v; break;
                case 2: ClosingQuantity_Month_3 = v; break;
                case 3: ClosingQuantity_Month_4 = v; break;
                case 4: ClosingQuantity_Month_5 = v; break;
                case 5: ClosingQuantity_Month_6 = v; break;
                case 6: ClosingQuantity_Month_7 = v; break;
                case 7: ClosingQuantity_Month_8 = v; break;
                case 8: ClosingQuantity_Month_9 = v; break;
                case 9: ClosingQuantity_Month_10 = v; break;
                case 10: ClosingQuantity_Month_11 = v; break;
                case 11: ClosingQuantity_Month_12 = v; break;
            }
        });
    }

    [Write(false)] public decimal[] MonthlyConsumptionValues { get; set; } = new decimal[12];
    [Write(false)] public decimal[] MonthlyClosingValues { get; set; } = new decimal[12];

    long IEntity.Id => AssessmentManufacturingMaterialId;
    string ISortableEntity.DisplayName => Description;

    private static void AssignMonthly(decimal[]? values, Action<int, decimal> assign)
    {
        if (values is null || values.Length != 12)
            throw new ArgumentException("Monthly values must have exactly 12 elements.");

        for (var index = 0; index < values.Length; index++)
            assign(index, values[index]);
    }
}
