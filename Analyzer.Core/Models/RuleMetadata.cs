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

}