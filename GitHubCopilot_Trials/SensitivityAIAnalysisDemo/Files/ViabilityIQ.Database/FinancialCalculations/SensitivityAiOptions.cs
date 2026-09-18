namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class SensitivityAiOptions
{
    public const string SectionName = "SensitivityAI";

    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/interactions";
    public string Model { get; set; } = "gemini-3.8-flash";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 45;
    public int MaxFindings { get; set; } = 6;
}
