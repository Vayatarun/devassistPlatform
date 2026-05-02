namespace Analyzer1.UI.Model;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? LastScan { get; set; }
    public string QualityGateStatus { get; set; } = "Unknown";
    public int IssueCount { get; set; }
}

public class CreateProjectRequest
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string? Description { get; set; }
}

public class MetricsDto
{
    public int TotalIssues { get; set; }
    public int Bugs { get; set; }
    public int Vulnerabilities { get; set; }
    public int CodeSmells { get; set; }
    public int CriticalCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public int TechnicalDebtMinutes { get; set; }
}

public class QualityGateDto
{
    public bool Passed { get; set; }
    public string Status { get; set; } = "Unknown";
    public List<string> FailureReasons { get; set; } = new();
    public int CriticalCount { get; set; }
    public int ErrorCount { get; set; }
    public int TotalIssues { get; set; }
    public int TechnicalDebtMinutes { get; set; }
}

public class ScanDto
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "";
    public int TotalIssues { get; set; }
    public int CriticalCount { get; set; }
    public int ErrorCount { get; set; }
    public bool QualityGatePassed { get; set; }
}

public class ScanResultDto
{
    public ScanDto? Scan { get; set; }
    public int IssueCount { get; set; }
    public int CriticalCount { get; set; }
    public bool QualityGatePassed { get; set; }
}

public class IssueDto
{
    public int Id { get; set; }
    public string Rule { get; set; } = "";
    public string RuleTitle { get; set; } = "";
    public string Category { get; set; } = "";
    public string Severity { get; set; } = "";
    public string FilePath { get; set; } = "";
    public int Line { get; set; }
    public string Message { get; set; } = "";
    public string? Description { get; set; }
    public string? Remediation { get; set; }
    public string? SuggestedFix { get; set; }
    public string? WhyItMatters { get; set; }
    public string? BadCodeExample { get; set; }
    public string? GoodCodeExample { get; set; }
    public int EffortMinutes { get; set; }
    public string Status { get; set; } = "Open";
    public int ProjectId { get; set; }
    public DateTime DetectedAt { get; set; }
}

public class IssuePageDto
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<IssueDto> Issues { get; set; } = new();
}

public class RuleDto
{
    public int Id { get; set; }
    public string RuleId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Severity { get; set; } = "";
    public bool Enabled { get; set; }
    public string? Description { get; set; }
    public string? Remediation { get; set; }
    public string? Tags { get; set; }
}

public class CategoryCount
{
    public string Category { get; set; } = "";
    public int Count { get; set; }
}

public class DashboardSummaryDto
{
    public int TotalProjects { get; set; }
    public int TotalIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int ErrorIssues { get; set; }
    public int WarningIssues { get; set; }
    public int InfoIssues { get; set; }
    public int TechnicalDebtMinutes { get; set; }
    public int ProjectsPassed { get; set; }
    public int ProjectsFailed { get; set; }
    public DateTime? LastScanAt { get; set; }
    public int LastScanIssues { get; set; }
    public List<CategoryCount> IssuesByCategory { get; set; } = new();
}
