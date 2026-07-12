using System;
using System.Collections.Generic;
using System.Linq;

namespace ViabilityIQ.Web.Components.Pages_Assessments.CommonComponents.WorkingCapital
{
    public static class WorkingCapitalEngine
    {
        public static void GenerateDistribution(WorkingCapitalModule module)
        {
            module.Distribution.Clear();
            module.MonthlyTotals.Clear();
            module.DistributionTotals.Clear();

            module.TotalInvoiced = 0;
            module.TotalDistributed = 0;

            var bucketTotals = new List<decimal>();

            for (int i = 0; i < 4; i++)
            {
                bucketTotals.Add(0);
            }

            foreach (var value in module.MonthlyValues)
            {
                var row = new WorkingCapitalDistributionRow
                {
                    Month = value.Month,
                    Invoiced = value.InvoicedAmount
                };

                row.Values.Add(value.InvoicedAmount * module.Profile.Days0To30 / 100m);
                row.Values.Add(value.InvoicedAmount * module.Profile.Days30To60 / 100m);
                row.Values.Add(value.InvoicedAmount * module.Profile.Days60To90 / 100m);
                row.Values.Add(value.InvoicedAmount * module.Profile.Days90To120 / 100m);

                module.Distribution.Add(row);

                module.TotalInvoiced += row.Invoiced;
                module.TotalDistributed += row.Total;

                for (int i = 0; i < row.Values.Count; i++)
                {
                    bucketTotals[i] += row.Values[i];
                }

                module.MonthlyTotals.Add(row.Total);
            }

            module.DistributionTotals.AddRange(bucketTotals);

            module.Summary.Outstanding = module.TotalDistributed;

            module.Summary.Percentage =
                module.TotalInvoiced == 0
                    ? 0
                    : Math.Round(
                        module.TotalDistributed /
                        module.TotalInvoiced * 100m,
                        2);

            module.Summary.Days =
                module.TotalInvoiced == 0
                    ? 0
                    : Math.Round(
                        module.TotalDistributed /
                        module.TotalInvoiced * 30m,
                        1);

            module.Summary.AnnualMovement = module.TotalInvoiced;
        }
    }
}