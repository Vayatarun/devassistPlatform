using Analyzer1.UI.Model;

namespace Analyzer1.UI.Services;

public interface IApiService
{
    Task<List<ProjectDto>> GetProjectsAsync();
    Task<ProjectDto?> CreateProjectAsync(CreateProjectRequest req);
    Task DeleteProjectAsync(int id);
    Task<MetricsDto?> GetMetricsAsync(int projectId);
    Task<QualityGateDto?> GetQualityGateAsync(int projectId);
    Task<List<ScanDto>> GetScanHistoryAsync(int projectId);
    Task<ScanResultDto?> RunScanAsync(int projectId);
    Task<IssuePageDto?> GetIssuesAsync(int projectId, string? severity = null, string? category = null, string? status = null, string? search = null, int page = 1);
    Task UpdateIssueStatusAsync(int issueId, string status);
    Task<List<RuleDto>> GetRulesAsync(string? category = null, string? search = null);
    Task UpdateRuleAsync(string ruleId, bool? enabled, string? severity);
    Task<DashboardSummaryDto?> GetDashboardAsync();
}
