using Analyzer.Core.Enums;

namespace Analyzer.Core.Models;

public class AnalysisMetrics
{
    public int TotalFiles { get; set; }
    public int FilesAnalyzed { get; set; }
    public int TotalIssues { get; set; }
    public int CriticalCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public int MajorCount { get; set; }
    public int MinorCount { get; set; }

    public int TechnicalDebtMinutes { get; set; }
    public string TechnicalDebtDisplay =>
        TechnicalDebtMinutes < 60
            ? $"{TechnicalDebtMinutes}min"
            : TechnicalDebtMinutes < 480
                ? $"{TechnicalDebtMinutes / 60}h {TechnicalDebtMinutes % 60}min"
                : $"{TechnicalDebtMinutes / 480}d {(TechnicalDebtMinutes % 480) / 60}h";

    public Dictionary<string, int> IssuesByCategory { get; set; } = new();
    public Dictionary<string, int> IssuesBySeverity { get; set; } = new();
    public Dictionary<string, int> IssuesByFile { get; set; } = new();

    public TimeSpan AnalysisDuration { get; set; }
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;

    public static AnalysisMetrics Calculate(IReadOnlyList<CodeIssue> issues, int totalFiles, TimeSpan duration)
    {
        var metrics = new AnalysisMetrics
        {
            TotalFiles = totalFiles,
            FilesAnalyzed = issues.Select(i => i.FilePath).Distinct().Count(),
            TotalIssues = issues.Count,
            CriticalCount = issues.Count(i => i.Severity == Severity.Critical),
            ErrorCount = issues.Count(i => i.Severity == Severity.Error),
            WarningCount = issues.Count(i => i.Severity == Severity.Warning),
            InfoCount = issues.Count(i => i.Severity == Severity.Info),
            MajorCount = issues.Count(i => i.Severity == Severity.Major),
            MinorCount = issues.Count(i => i.Severity == Severity.Minor),
            TechnicalDebtMinutes = issues.Sum(i => i.EffortMinutes),
            AnalysisDuration = duration,
            IssuesByCategory = issues
                .GroupBy(i => string.IsNullOrEmpty(i.Category) ? "General" : i.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            IssuesBySeverity = issues
                .GroupBy(i => i.Severity.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            IssuesByFile = issues
                .GroupBy(i => Path.GetFileName(i.FilePath))
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count())
        };
        return metrics;
    }
}
