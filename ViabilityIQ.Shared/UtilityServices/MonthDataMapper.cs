using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Shared.UtilityServices
{
    public static class MonthDataMapper
    {
        public static void SyncToModel(decimal[] values, AssessmentSales model)
        {
            model.Month_1 = values[0]; model.Month_2 = values[1];
            model.Month_3 = values[2]; model.Month_4 = values[3];
            model.Month_5 = values[4]; model.Month_6 = values[5];
            model.Month_7 = values[6]; model.Month_8 = values[7];
            model.Month_9 = values[8]; model.Month_10 = values[9];
            model.Month_11 = values[10]; model.Month_12 = values[11];
        }

        public static decimal[] SyncToArray(AssessmentSales model)
        {
            return new decimal[] {
                model.Month_1, model.Month_2, model.Month_3, model.Month_4,
                model.Month_5, model.Month_6, model.Month_7, model.Month_8,
                model.Month_9, model.Month_10, model.Month_11, model.Month_12
            };
        }
    }
}
