using Analyzer.Core.Enums;

namespace Analyzer.Core.Models;

public record CodeIssue
{
    public string RuleId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public int Column { get; init; }

    public Severity Severity { get; init; }

    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}