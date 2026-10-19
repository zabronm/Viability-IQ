using System;
using System.Collections.Generic;
using System.Globalization;
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


        //===  Get disolay color if either positive or negative
        public static string GetDisplayColorByValue(decimal value,
                                                    decimal comparisonValue = 0,
                                                    string positiveColor = "#0284c7",
                                                    string negativeColor = "#dc2626",
                                                    string equalColor = "#6b7280")
        {
            if (value > comparisonValue) return positiveColor;
            if (value < comparisonValue) return negativeColor;
            return equalColor;
        }


        //===== is the number positive?
        public static bool IsPositive(decimal value)
        {
            return value >= 0;
        }

        //== format to local currency and display ====
        public static string FormatCurrency(decimal value)
        {
            return value.ToString("N2");
        }

        public static string FormatCurrency(
            decimal value,
            string? currencyCode,
            string? cultureName)
        {
            var culture = GetCulture(cultureName);
            var currency = string.IsNullOrWhiteSpace(currencyCode)
                ? "ZAR"
                : currencyCode.Trim().ToUpperInvariant();
            return $"{currency} {value.ToString("N2", culture)}";
        }

        public static string FormatNumber(decimal value, string? cultureName, string format = "N2") =>
            value.ToString(format, GetCulture(cultureName));

        public static string FormatDate(DateTime value, string? cultureName) =>
            value.ToString("d", GetCulture(cultureName));

        private static CultureInfo GetCulture(string? cultureName)
        {
            try
            {
                return CultureInfo.GetCultureInfo(
                    string.IsNullOrWhiteSpace(cultureName) ? "en-ZA" : cultureName);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.GetCultureInfo("en-ZA");
            }
        }

        public static string FormatPercentage(decimal value)
        {
            return $"{value:N2}%";
        }

        public static string GetTrendIcon(decimal value)
        {
            return value >= 0
                ? "bi bi-arrow-up-circle-fill"
                : "bi bi-arrow-down-circle-fill";
        }

        public static string GetTrendColor(decimal value)
        {
            return value >= 0
                ? "#16a34a"
                : "#dc2626";
        }
    }

}
