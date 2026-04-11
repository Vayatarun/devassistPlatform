using Analyzer.Core.Enums;

namespace Analyzer.Core.Models;

public record RuleConfiguration
{
    public string RuleId { get; init; } = string.Empty;
    public bool IsEnabled { get; set; } = true;

    public Severity? OverrideSeverity { get; set; }

    public Dictionary<string, string> Parameters { get; set; } = new();
}