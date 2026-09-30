namespace ViabilityIQ.Shared.Reporting;

public static class ProjectPeriodMapper
{
    private static readonly string[] MonthNames =
        ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sept", "Oct", "Nov", "Dec"];

    public static ProjectPeriodMonth GetMonth(DateTime? assessmentStartDate, int projectMonth)
    {
        if (projectMonth is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(projectMonth), "Project month must be between 1 and 12.");

        var projectLabel = $"M-{projectMonth}";
        if (!assessmentStartDate.HasValue)
            return new ProjectPeriodMonth(projectMonth, projectLabel, null, projectLabel);

        var start = assessmentStartDate.Value;
        var calendarMonth = new DateTime(start.Year, start.Month, 1).AddMonths(projectMonth - 1);
        var calendarLabel = $"{MonthNames[calendarMonth.Month - 1]}/{calendarMonth:yy}";
        return new ProjectPeriodMonth(
            projectMonth,
            projectLabel,
            calendarMonth,
            calendarLabel);
    }

    public static string GetRangeLabel(DateTime? assessmentStartDate, int startMonth, int endMonth)
    {
        if (startMonth > endMonth)
            throw new ArgumentOutOfRangeException(nameof(startMonth), "The start month must not be after the end month.");

        var start = GetMonth(assessmentStartDate, startMonth);
        var end = GetMonth(assessmentStartDate, endMonth);
        return $"{start.CalendarLabel} – {end.CalendarLabel}";
    }

    public static string GetTableLabel(DateTime? assessmentStartDate, int projectMonth)
    {
        var month = GetMonth(assessmentStartDate, projectMonth);
        return month.CalendarMonth.HasValue
            ? $"{month.ProjectLabel} {month.CalendarLabel}"
            : month.ProjectLabel;
    }
}

public sealed record ProjectPeriodMonth(
    int ProjectMonth,
    string ProjectLabel,
    DateTime? CalendarMonth,
    string CalendarLabel);
