using Analyzer.Core.Enums;

namespace Analyzer.Core.Models;

public record CodeIssue
{
    public string RuleId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public int Column { get; init; }

    public Severity Severity { get; init; }

    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;

    public string Remediation { get; init; } = string.Empty;
    public int EffortMinutes { get; init; } = 5;
    public string[] Tags { get; init; } = Array.Empty<string>();

    public string SuggestedFix { get; init; } = string.Empty;
    public string WhyItMatters { get; init; } = string.Empty;
    public string BadCodeExample { get; init; } = string.Empty;
    public string GoodCodeExample { get; init; } = string.Empty;
}