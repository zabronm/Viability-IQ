namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class GroqSensitivityAiOptions
{
    public const string SectionName = "GroqSensitivityAI";

    public bool Enabled { get; set; }
    public string Endpoint { get; set; } =
        "https://api.groq.com/openai/v1/chat/completions";
    public string Model { get; set; } = "openai/gpt-oss-20b";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxFindings { get; set; } = 6;
    public int MaxCompletionTokens { get; set; } = 2500;
}

