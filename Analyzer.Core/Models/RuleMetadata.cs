using Analyzer.Core.Enums;

namespace Analyzer.Core.Models;

public record RuleMetadata
{
    public string RuleId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public Severity DefaultSeverity { get; init; }
    public RuleExecutionPhase Phase { get; set; } = RuleExecutionPhase.Analyzer;

    public string Remediation { get; init; } = string.Empty;
    public int EffortMinutes { get; init; } = 5;
    public string[] Tags { get; init; } = Array.Empty<string>();
    public string DocumentationUrl { get; init; } = string.Empty;

    public string WhyItMatters { get; init; } = string.Empty;
    public string BadCodeExample { get; init; } = string.Empty;
    public string GoodCodeExample { get; init; } = string.Empty;
}