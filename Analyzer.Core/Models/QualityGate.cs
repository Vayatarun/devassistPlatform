namespace Analyzer.Core.Models;

public class QualityGateConfig
{
    public int MaxCriticalIssues { get; set; } = 0;
    public int MaxErrorIssues { get; set; } = 5;
    public int MaxTotalIssues { get; set; } = int.MaxValue;
    public int MaxTechnicalDebtMinutes { get; set; } = 480;
}

public class QualityGateResult
{
    public bool Passed { get; set; }
    public List<string> FailureReasons { get; set; } = new();
    public AnalysisMetrics Metrics { get; set; } = new();

    public static QualityGateResult Evaluate(AnalysisMetrics metrics, QualityGateConfig config)
    {
        var result = new QualityGateResult { Metrics = metrics, Passed = true };

        if (metrics.CriticalCount > config.MaxCriticalIssues)
        {
            result.Passed = false;
            result.FailureReasons.Add(
                $"Critical issues: {metrics.CriticalCount} (max {config.MaxCriticalIssues})");
        }

        if (metrics.ErrorCount > config.MaxErrorIssues)
        {
            result.Passed = false;
            result.FailureReasons.Add(
                $"Error issues: {metrics.ErrorCount} (max {config.MaxErrorIssues})");
        }

        if (metrics.TotalIssues > config.MaxTotalIssues)
        {
            result.Passed = false;
            result.FailureReasons.Add(
                $"Total issues: {metrics.TotalIssues} (max {config.MaxTotalIssues})");
        }

        if (metrics.TechnicalDebtMinutes > config.MaxTechnicalDebtMinutes)
        {
            result.Passed = false;
            result.FailureReasons.Add(
                $"Technical debt: {metrics.TechnicalDebtDisplay} (max {config.MaxTechnicalDebtMinutes / 60}h)");
        }

        return result;
    }
}
