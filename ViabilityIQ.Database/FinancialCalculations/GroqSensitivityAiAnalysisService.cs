using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations;

public sealed class GroqSensitivityAiAnalysisService(
    HttpClient httpClient,
    IOptions<GroqSensitivityAiOptions> options,
    ILogger<GroqSensitivityAiAnalysisService> logger)
    : IGroqSensitivityAiAnalysisService
{
    private const string Provider = "GroqCloud";
    private static readonly HashSet<string> Severities =
        new(["Critical", "High", "Medium", "Low"], StringComparer.Ordinal);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly GroqSensitivityAiOptions _options = options.Value;

    public SensitivityAiConfiguration GetConfiguration() => new(
        _options.Enabled,
        _options.Enabled
            && !string.IsNullOrWhiteSpace(_options.ApiKey)
            && Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == Uri.UriSchemeHttps
            && endpoint.Host.Equals("api.groq.com", StringComparison.OrdinalIgnoreCase),
        Provider,
        _options.Model,
        Math.Clamp(_options.MaxFindings, 1, 10));

    public async Task<SensitivityAiAnalysisResult> AnalyseAsync(
        SensitivityAiAnalysisRequest request,
        bool consentGiven,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correlationId = Guid.NewGuid().ToString("N");
        var configuration = GetConfiguration();
        if (!configuration.Enabled)
            return Failure(SensitivityAiAnalysisStatus.Disabled, "GroqCloud analysis is disabled.", correlationId);
        if (!configuration.Configured)
            return Failure(SensitivityAiAnalysisStatus.Unconfigured, "GroqCloud analysis is not configured.", correlationId);
        if (!consentGiven)
            return Failure(SensitivityAiAnalysisStatus.ConsentRequired, "Consent is required before sending calculated metrics to GroqCloud.", correlationId);

        var evidence = BuildEvidence(request);
        if (evidence.Count == 0)
            return Failure(SensitivityAiAnalysisStatus.InvalidResponse, "There are no calculated metrics available to analyse.", correlationId);

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
            message.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            message.Content = JsonContent.Create(
                BuildProviderRequest(request, evidence.Keys, configuration.MaxFindings),
                options: JsonOptions);

            using var response = await httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return LoggedFailure(SensitivityAiAnalysisStatus.Unauthorized, "GroqCloud rejected the configured API key.", correlationId, "authentication");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return LoggedFailure(SensitivityAiAnalysisStatus.RateLimited, "The GroqCloud rate limit was reached. Please try again later.", correlationId, "rate_limit");
            if (response.StatusCode == HttpStatusCode.BadRequest)
                return LoggedFailure(SensitivityAiAnalysisStatus.InvalidResponse, "GroqCloud rejected the request. Verify that the configured model supports structured output.", correlationId, "bad_request");
            if (!response.IsSuccessStatusCode)
                return LoggedFailure(SensitivityAiAnalysisStatus.ServiceUnavailable, "GroqCloud could not complete the analysis. Please try again later.", correlationId, $"http_{(int)response.StatusCode}");

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var responseJson = await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);
            var modelJson = ExtractModelJson(responseJson.RootElement);
            if (modelJson is null)
                return LoggedFailure(SensitivityAiAnalysisStatus.InvalidResponse, "GroqCloud returned an unreadable response.", correlationId, "response_shape");

            RawAnalysis? raw;
            try
            {
                raw = JsonSerializer.Deserialize<RawAnalysis>(modelJson, JsonOptions);
            }
            catch (JsonException)
            {
                return LoggedFailure(SensitivityAiAnalysisStatus.InvalidResponse, "GroqCloud returned malformed analysis data.", correlationId, "malformed_json");
            }

            var validationError = Validate(raw, evidence, configuration.MaxFindings);
            if (validationError is not null)
                return LoggedFailure(SensitivityAiAnalysisStatus.InvalidResponse, "GroqCloud returned analysis data that failed validation.", correlationId, validationError);

            var findings = raw!.Findings!
                .OrderBy(finding => finding.Priority)
                .Select(finding => new SensitivityAiFinding(
                    finding.Priority,
                    finding.Area!.Trim(),
                    finding.Severity!,
                    finding.Finding!.Trim(),
                    string.Join("; ", finding.EvidenceKeys!.Select(key => evidence[key])),
                    finding.Recommendation!.Trim(),
                    finding.Confidence))
                .ToArray();

            logger.LogInformation(
                "Sensitivity AI provider {Provider}, model {Model}, outcome {Outcome}, correlation {CorrelationId}",
                Provider, configuration.Model, "success", correlationId);

            return new SensitivityAiAnalysisResult
            {
                Status = SensitivityAiAnalysisStatus.Success,
                UserMessage = "Analysis generated.",
                Provider = Provider,
                Model = configuration.Model,
                CorrelationId = correlationId,
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                ExecutiveSummary = raw.ExecutiveSummary!.Trim(),
                Findings = findings
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return LoggedFailure(SensitivityAiAnalysisStatus.Cancelled, "GroqCloud analysis was cancelled.", correlationId, "cancelled");
        }
        catch (OperationCanceledException)
        {
            return LoggedFailure(SensitivityAiAnalysisStatus.Timeout, "GroqCloud did not respond within the configured time limit.", correlationId, "timeout");
        }
        catch (HttpRequestException)
        {
            return LoggedFailure(SensitivityAiAnalysisStatus.ServiceUnavailable, "GroqCloud is currently unavailable. Please try again later.", correlationId, "network");
        }
        catch (JsonException)
        {
            return LoggedFailure(SensitivityAiAnalysisStatus.InvalidResponse, "GroqCloud returned malformed response data.", correlationId, "envelope_json");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unexpected GroqCloud sensitivity failure, correlation {CorrelationId}",
                correlationId);
            return Failure(SensitivityAiAnalysisStatus.ServiceUnavailable, "GroqCloud could not complete the analysis. Please try again later.", correlationId);
        }
    }

    private object BuildProviderRequest(
        SensitivityAiAnalysisRequest request,
        IEnumerable<string> evidenceKeys,
        int maxFindings)
    {
        var keys = evidenceKeys.ToArray();
        var schema = new
        {
            type = "object",
            additionalProperties = false,
            required = new[] { "executiveSummary", "findings" },
            properties = new
            {
                executiveSummary = new { type = "string" },
                findings = new
                {
                    type = "array",
                    minItems = 1,
                    maxItems = maxFindings,
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "priority", "area", "severity", "finding", "evidenceKeys", "recommendation", "confidence" },
                        properties = new
                        {
                            priority = new { type = "integer", minimum = 1, maximum = maxFindings },
                            area = new { type = "string" },
                            severity = new { type = "string", @enum = Severities.ToArray() },
                            finding = new { type = "string" },
                            evidenceKeys = new
                            {
                                type = "array",
                                minItems = 1,
                                maxItems = 8,
                                items = new { type = "string" }
                            },
                            recommendation = new { type = "string" },
                            confidence = new { type = "integer", minimum = 0, maximum = 100 }
                        }
                    }
                }
            }
        };

        var systemInstruction =
            $"You are an SME financial assessment and sensitivity analyst. Produce an executive interpretation and no more than {maxFindings} prioritized recommendations. " +
            "Cover profitability, liquidity, working capital, debt capacity, break-even resilience and scenario concentration where supported. " +
            "Use only supplied calculated metrics, deterministic flags and adjustments. Never invent facts, identities or figures and never recalculate values. " +
            "Narrative fields must contain no digits because numeric evidence must be referenced only through evidenceKeys. " +
            "Every evidence key must come from the allow-list. Return JSON only.";

        var userInput = JsonSerializer.Serialize(
            new
            {
                evidenceKeyAllowList = keys,
                calculatedInput = request
            },
            JsonOptions);

        return new
        {
            model = _options.Model,
            temperature = 0.15,
            max_completion_tokens = Math.Clamp(
                _options.MaxCompletionTokens,
                500,
                6000),
            messages = new object[]
            {
                new { role = "system", content = systemInstruction },
                new { role = "user", content = userInput }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "sensitivity_analysis",
                    strict = true,
                    schema
                }
            }
        };
    }

    private static Dictionary<string, string> BuildEvidence(
        SensitivityAiAnalysisRequest request)
    {
        var evidence = new Dictionary<string, string>(StringComparer.Ordinal);
        AddMetrics(evidence, "current", request.Metrics);
        AddMetrics(evidence, "baseline", request.BaselineMetrics);

        foreach (var month in request.Months
                     .Where(month => month.Month is >= 1 and <= 12)
                     .Take(12))
        {
            var prefix = $"month.m{month.Month}";
            evidence[$"{prefix}.revenue"] = $"M{month.Month} revenue: {Money(month.Revenue)}";
            evidence[$"{prefix}.profitBeforeTax"] = $"M{month.Month} profit before tax: {Money(month.ProfitBeforeTax)}";
            evidence[$"{prefix}.closingCash"] = $"M{month.Month} closing cash: {Money(month.ClosingCash)}";
            evidence[$"{prefix}.netCashflow"] = $"M{month.Month} net cash flow: {Money(month.NetCashflow)}";
            evidence[$"{prefix}.closingDebtors"] = $"M{month.Month} closing debtors: {Money(month.ClosingDebtors)}";
            evidence[$"{prefix}.closingCreditors"] = $"M{month.Month} closing creditors: {Money(month.ClosingCreditors)}";
        }

        for (var index = 0; index < request.DeterministicFlags.Count; index++)
            evidence[$"deterministic.flag.{index + 1}"] =
                $"Deterministic flag: {request.DeterministicFlags[index]}";
        for (var index = 0; index < request.Adjustments.Count; index++)
        {
            var adjustment = request.Adjustments[index];
            evidence[$"adjustment.{index + 1}"] =
                $"{adjustment.Driver}: {adjustment.Value.ToString("0.##", CultureInfo.InvariantCulture)}, effective M{adjustment.EffectiveStartMonth}" +
                (adjustment.DurationMonths.HasValue
                    ? $" for {adjustment.DurationMonths} months"
                    : " onward");
        }

        return evidence;
    }

    private static void AddMetrics(
        IDictionary<string, string> evidence,
        string prefix,
        SensitivityAiMetricSnapshot metrics)
    {
        evidence[$"{prefix}.revenue"] = $"{Label(prefix)} revenue: {Money(metrics.Revenue)}";
        evidence[$"{prefix}.grossProfit"] = $"{Label(prefix)} gross profit: {Money(metrics.GrossProfit)}";
        evidence[$"{prefix}.ebitda"] = $"{Label(prefix)} EBITDA: {Money(metrics.EBITDA)}";
        evidence[$"{prefix}.profitBeforeTax"] = $"{Label(prefix)} profit before tax: {Money(metrics.ProfitBeforeTax)}";
        evidence[$"{prefix}.closingCash"] = $"{Label(prefix)} closing cash: {Money(metrics.ClosingCash)}";
        evidence[$"{prefix}.minimumCash"] = $"{Label(prefix)} minimum cash: {Money(metrics.MinimumCash)} in M{metrics.MinimumCashMonth}";
        evidence[$"{prefix}.fundingShortfall"] = $"{Label(prefix)} funding shortfall: {Money(metrics.FundingShortfall)}";
        AddOptional(evidence, $"{prefix}.grossMarginPercent", $"{Label(prefix)} gross margin", metrics.GrossMarginPercent, "%");
        AddOptional(evidence, $"{prefix}.currentRatio", $"{Label(prefix)} current ratio", metrics.CurrentRatio);
        AddOptional(evidence, $"{prefix}.interestCover", $"{Label(prefix)} interest cover", metrics.InterestCover);
        AddOptional(evidence, $"{prefix}.breakEvenSales", $"{Label(prefix)} break-even sales", metrics.BreakEvenSales, money: true);
        AddOptional(evidence, $"{prefix}.marginOfSafety", $"{Label(prefix)} margin of safety", metrics.MarginOfSafety, "%");
    }

    private static void AddOptional(
        IDictionary<string, string> evidence,
        string key,
        string label,
        decimal? value,
        string suffix = "",
        bool money = false)
    {
        if (value.HasValue)
            evidence[key] =
                $"{label}: {(money ? Money(value.Value) : value.Value.ToString("N2", CultureInfo.InvariantCulture) + suffix)}";
    }

    private static string? Validate(
        RawAnalysis? analysis,
        IReadOnlyDictionary<string, string> evidence,
        int maxFindings)
    {
        if (analysis is null
            || InvalidText(analysis.ExecutiveSummary, 1200)
            || ContainsDigit(analysis.ExecutiveSummary))
            return "summary";
        if (analysis.Findings is null
            || analysis.Findings.Count is < 1
            || analysis.Findings.Count > maxFindings)
            return "finding_count";

        var rows = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var finding in analysis.Findings)
        {
            if (finding.Priority is < 1
                || finding.Priority > maxFindings
                || InvalidText(finding.Area, 80)
                || ContainsDigit(finding.Area)
                || !Severities.Contains(finding.Severity ?? string.Empty)
                || InvalidText(finding.Finding, 600)
                || ContainsDigit(finding.Finding)
                || InvalidText(finding.Recommendation, 600)
                || ContainsDigit(finding.Recommendation)
                || finding.Confidence is < 0 or > 100
                || finding.EvidenceKeys is null
                || finding.EvidenceKeys.Count is < 1 or > 8
                || finding.EvidenceKeys.Distinct(StringComparer.Ordinal).Count()
                    != finding.EvidenceKeys.Count
                || finding.EvidenceKeys.Any(key => !evidence.ContainsKey(key)))
                return "finding_fields";

            var rowKey =
                $"{finding.Area?.Trim()}|{finding.Severity}|{finding.Finding?.Trim()}|{finding.Recommendation?.Trim()}";
            if (!rows.Add(rowKey))
                return "duplicate_finding";
        }

        return null;
    }

    private static string? ExtractModelJson(JsonElement root)
    {
        if (!root.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var choice in choices.EnumerateArray())
        {
            if (choice.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String)
                return content.GetString();
        }

        return null;
    }

    private SensitivityAiAnalysisResult Failure(
        SensitivityAiAnalysisStatus status,
        string message,
        string correlationId) => new()
        {
            Status = status,
            UserMessage = message,
            Provider = Provider,
            Model = _options.Model,
            CorrelationId = correlationId
        };

    private SensitivityAiAnalysisResult LoggedFailure(
        SensitivityAiAnalysisStatus status,
        string message,
        string correlationId,
        string category)
    {
        logger.LogWarning(
            "Sensitivity AI provider {Provider}, model {Model}, outcome {Outcome}, correlation {CorrelationId}",
            Provider, _options.Model, category, correlationId);
        return Failure(status, message, correlationId);
    }

    private static bool InvalidText(string? value, int maximum) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length > maximum;

    private static bool ContainsDigit(string? value) =>
        value?.Any(char.IsDigit) == true;

    private static string Label(string prefix) =>
        prefix == "current" ? "Current case" : "Baseline";

    private static string Money(decimal value) =>
        value.ToString("N2", CultureInfo.InvariantCulture);

    private sealed class RawAnalysis
    {
        public string? ExecutiveSummary { get; init; }
        public List<RawFinding>? Findings { get; init; }
    }

    private sealed class RawFinding
    {
        public int Priority { get; init; }
        public string? Area { get; init; }
        public string? Severity { get; init; }
        public string? Finding { get; init; }
        public List<string>? EvidenceKeys { get; init; }
        public string? Recommendation { get; init; }
        public int Confidence { get; init; }
    }
}
