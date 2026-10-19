using System.Globalization;

namespace ViabilityIQ.Shared.SharedModels;

public sealed record AssessmentLocaleOption(
    string CultureName,
    string CountryCode,
    string DisplayName,
    string DefaultCurrencyCode);

public static class AssessmentLocaleOptions
{
    public static IReadOnlyList<AssessmentLocaleOption> EnglishCultures { get; } =
        BuildEnglishCultures();

    public static IReadOnlyList<string> CurrencyCodes { get; } =
        EnglishCultures
            .Select(option => option.DefaultCurrencyCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<AssessmentLocaleOption> BuildEnglishCultures()
    {
        var options = new List<AssessmentLocaleOption>();
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                     .Where(item => item.Name.StartsWith("en-", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var region = new RegionInfo(culture.Name);
                if (region.TwoLetterISORegionName.Length != 2
                    || region.ISOCurrencySymbol.Length != 3)
                {
                    continue;
                }

                options.Add(new(
                    culture.Name,
                    region.TwoLetterISORegionName,
                    $"{region.EnglishName} (English - {culture.Name})",
                    region.ISOCurrencySymbol));
            }
            catch (ArgumentException)
            {
                // Some framework cultures do not map to an ISO country/region.
            }
        }

        return options
            .GroupBy(option => option.CultureName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
