namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class SensitivityAiOptions
{
    public const string SectionName = "SensitivityAI";

    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent";
    public string Model { get; set; } = "gemini-3.6-flash";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 45;
    public int MaxFindings { get; set; } = 6;
}