using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public static class ManufacturingProjectionCalculator
{
    private const int ProjectionMonths = 12;

    public static IReadOnlyList<ManufacturingProjectionMonth> Calculate(
        IReadOnlyList<AssessmentManufacturingMaterial> materials,
        IReadOnlyList<AssessmentManufacturingBalances> balances,
        IReadOnlyList<AssessmentExpenses> expenses,
        IReadOnlyList<AssessmentSalesCategory> categories,
        ICollection<CashflowProjectionAlert> alerts)
    {
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(balances);
        ArgumentNullException.ThrowIfNull(expenses);
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(alerts);

        if (materials.Count == 0)
        {
            alerts.Add(new(
                "manufacturing-materials-missing",
                CashflowAlertSeverity.Warning,
                "Raw-material estimates missing",
                "No raw-material items are configured. Raw-material purchases and consumption are shown as zero until material estimates are entered."));
        }
        else if (materials.All(x =>
                     x.OpeningQuantity == 0m
                     && x.OpeningValue == 0m
                     && x.MonthlyPurchaseQuantities.All(value => value == 0m)
                     && x.MonthlyPurchaseValues.All(value => value == 0m)
                     && x.MonthlyClosingQuantities.All(value => value == 0m)))
        {
            alerts.Add(new(
                "manufacturing-material-estimates-empty",
                CashflowAlertSeverity.Warning,
                "Raw-material estimates are all zero",
                "Raw-material items exist, but no opening balance, purchase or closing quantity is entered. Confirm these zero values are intentional."));
        }

        var missingBalanceCategories = categories
            .Where(category => balances.All(balance =>
                balance.AssessmentSalesCategoryId != category.AssessmentSalesCategoryId))
            .Select(category => category.AssessmentSalesCategoryName)
            .ToArray();
        if (balances.Count == 0 || missingBalanceCategories.Length > 0)
        {
            alerts.Add(new(
                "manufacturing-balances-missing",
                CashflowAlertSeverity.Warning,
                "Category inventory estimates missing",
                missingBalanceCategories.Length == 0
                    ? "Opening work in progress and finished goods, and monthly closing balances, are not configured. Those balances are currently treated as zero."
                    : $"WIP and finished-goods balances are missing for: {string.Join(", ", missingBalanceCategories)}. Those category balances are currently treated as zero."));
        }
        if (balances.Any(x => !x.InventoryEstimatesConfirmed))
        {
            alerts.Add(new(
                "manufacturing-balances-unconfirmed",
                CashflowAlertSeverity.Warning,
                "Inventory estimates need confirmation",
                "Review opening WIP and finished goods and each month's closing WIP and finished-goods values for every sales category. Confirm when zero balances are intentional."));
        }

        var results = new List<ManufacturingProjectionMonth>(ProjectionMonths);
        var rawMaterialOpeningQuantity = materials.ToDictionary(
            x => x.AssessmentManufacturingMaterialId,
            x => x.OpeningQuantity);
        var rawMaterialOpeningValue = materials.ToDictionary(
            x => x.AssessmentManufacturingMaterialId,
            x => x.OpeningValue);
        var materialClosingValues = materials.ToDictionary(
            x => x.AssessmentManufacturingMaterialId,
            CalculateMonthlyClosingValues);
        var openingWorkInProgress = balances.Sum(x => x.OpeningWorkInProgress);
        var openingFinishedGoods = balances.Sum(x => x.OpeningFinishedGoods);

        for (var month = 0; month < ProjectionMonths; month++)
        {
            var purchaseValue = 0m;
            var purchaseGross = 0m;
            var rawMaterialConsumption = 0m;
            var closingRawMaterials = 0m;
            var openingRawMaterials = rawMaterialOpeningValue.Values.Sum();

            foreach (var material in materials)
            {
                var purchaseQuantity = Value(material.MonthlyPurchaseQuantities, month);
                var purchases = Value(material.MonthlyPurchaseValues, month);
                var closingQuantity = Value(material.MonthlyClosingQuantities, month);
                var openingQuantity = rawMaterialOpeningQuantity[material.AssessmentManufacturingMaterialId];
                var openingValue = rawMaterialOpeningValue[material.AssessmentManufacturingMaterialId];
                var availableQuantity = openingQuantity + purchaseQuantity;
                var availableValue = openingValue + purchases;
                var weightedAverageCost = availableQuantity > 0m
                    ? availableValue / availableQuantity
                    : 0m;
                var consumedQuantity = availableQuantity - closingQuantity;
                var consumedValue = consumedQuantity * weightedAverageCost;
                var closingValue = materialClosingValues[material.AssessmentManufacturingMaterialId][month];

                if (purchaseQuantity < 0m || purchases < 0m || closingQuantity < 0m)
                {
                    AddMaterialAlert(alerts, material, month,
                        "Negative raw-material quantity or purchase value was entered.");
                }

                if ((purchaseQuantity == 0m) != (purchases == 0m))
                {
                    AddMaterialAlert(
                        alerts,
                        material,
                        month,
                        "Purchase quantity and purchase value do not both contain a positive estimate.");
                }

                if (closingQuantity > availableQuantity)
                {
                    AddMaterialAlert(
                        alerts,
                        material,
                        month,
                        $"Closing quantity exceeds opening quantity plus purchases by {closingQuantity - availableQuantity:N2} {material.UnitOfMeasure}.");
                }

                if (availableQuantity <= 0m && closingQuantity > 0m)
                {
                    AddMaterialAlert(
                        alerts,
                        material,
                        month,
                        "Closing quantity has no available cost basis; its projected value is zero.");
                }

                purchaseValue += purchases;
                purchaseGross += GrossPurchaseValue(material, purchases);
                rawMaterialConsumption += consumedValue;
                closingRawMaterials += closingValue;
                rawMaterialOpeningQuantity[material.AssessmentManufacturingMaterialId] = closingQuantity;
                rawMaterialOpeningValue[material.AssessmentManufacturingMaterialId] = closingValue;
            }

            var productionCostNet = expenses
                .Where(x => x.IsDirectProductionCost)
                .Sum(x => MonthValue(x.MonthlyValues, month));

            var closingWorkInProgress = balances.Sum(
                x => Value(x.MonthlyClosingWorkInProgress, month));
            var closingFinishedGoods = balances.Sum(
                x => Value(x.MonthlyClosingFinishedGoods, month));
            var completedProductionCost =
                openingWorkInProgress + rawMaterialConsumption + productionCostNet - closingWorkInProgress;
            var costOfGoodsSold =
                openingFinishedGoods + completedProductionCost - closingFinishedGoods;

            if (closingWorkInProgress < 0m || closingFinishedGoods < 0m)
            {
                alerts.Add(new(
                    $"manufacturing-negative-closing-balance-{month + 1}",
                    CashflowAlertSeverity.Warning,
                    "Negative manufacturing inventory estimate",
                    $"Month {month + 1} has a negative closing WIP or finished-goods value. Review the entered balances."));
            }

            if (completedProductionCost < 0m)
            {
                alerts.Add(new(
                    $"manufacturing-wip-reconciliation-{month + 1}",
                    CashflowAlertSeverity.Warning,
                    "WIP estimate exceeds available production cost",
                    $"Month {month + 1} closing WIP exceeds opening WIP plus materials consumed and production costs by {Math.Abs(completedProductionCost):N2}. Cost transferred to finished goods is therefore negative; review the WIP estimate."));
            }

            if (costOfGoodsSold < 0m)
            {
                alerts.Add(new(
                    $"manufacturing-finished-goods-reconciliation-{month + 1}",
                    CashflowAlertSeverity.Warning,
                    "Finished-goods estimate exceeds available cost",
                    $"Month {month + 1} closing finished goods exceed opening finished goods plus completed production cost by {Math.Abs(costOfGoodsSold):N2}. Cost of sales is therefore negative; review the closing balance."));
            }

            results.Add(new ManufacturingProjectionMonth(
                month + 1,
                purchaseValue,
                purchaseGross,
                openingRawMaterials,
                rawMaterialConsumption,
                closingRawMaterials,
                productionCostNet,
                openingWorkInProgress,
                closingWorkInProgress,
                completedProductionCost,
                openingFinishedGoods,
                closingFinishedGoods,
                costOfGoodsSold,
                costOfGoodsSold));

            openingWorkInProgress = closingWorkInProgress;
            openingFinishedGoods = closingFinishedGoods;
        }

        return results;
    }

    public static decimal[] CalculateMonthlyClosingValues(
        AssessmentManufacturingMaterial material)
    {
        ArgumentNullException.ThrowIfNull(material);

        var closingValues = new decimal[ProjectionMonths];
        var openingQuantity = material.OpeningQuantity;
        var openingValue = material.OpeningValue;

        for (var month = 0; month < ProjectionMonths; month++)
        {
            var availableQuantity = openingQuantity
                + Value(material.MonthlyPurchaseQuantities, month);
            var availableValue = openingValue
                + Value(material.MonthlyPurchaseValues, month);
            var weightedAverageCost = availableQuantity > 0m
                ? availableValue / availableQuantity
                : 0m;
            var closingQuantity = Value(material.MonthlyClosingQuantities, month);
            var closingValue = closingQuantity * weightedAverageCost;

            closingValues[month] = closingValue;
            openingQuantity = closingQuantity;
            openingValue = closingValue;
        }

        return closingValues;
    }

    private static decimal GrossPurchaseValue(AssessmentManufacturingMaterial material, decimal net)
    {
        if (!material.IncludeVAT || net == 0m)
            return net;
        return material.TotalNoVAT > 0m && material.TotalWithVAT >= material.TotalNoVAT
            ? net * material.TotalWithVAT / material.TotalNoVAT
            : net * (1m + Math.Max(0m, material.VATRate) / 100m);
    }

    private static decimal Value(decimal[] values, int index) =>
        values.Length > index ? values[index] : 0m;

    private static decimal MonthValue(decimal[] values, int index) =>
        values.Length > index ? values[index] : 0m;

    private static void AddMaterialAlert(
        ICollection<CashflowProjectionAlert> alerts,
        AssessmentManufacturingMaterial material,
        int month,
        string message)
    {
        alerts.Add(new(
            $"manufacturing-material-{material.AssessmentManufacturingMaterialId}-{month + 1}-{alerts.Count}",
            CashflowAlertSeverity.Warning,
            $"Review {material.Description} balance",
            $"Month {month + 1}: {message}"));
    }
}
