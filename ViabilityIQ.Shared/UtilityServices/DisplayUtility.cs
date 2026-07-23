using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ViabilityIQ.Shared.UtilityServices
{
    public static class DisplayUtility
    {

        //==========  Truncates long string values to a certain number of characters ===========
        public static string TruncateValue(string? value, int maxLength = 20)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (value.Length <= maxLength)
                return value;

            return value[..maxLength] + "..";
        }
    }
}
