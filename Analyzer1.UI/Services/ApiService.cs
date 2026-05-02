using Analyzer1.UI.Model;
using System.Net.Http.Json;

namespace Analyzer1.UI.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http) => _http = http;

    public async Task<List<ProjectDto>> GetProjectsAsync()
    {
        try { return await _http.GetFromJsonAsync<List<ProjectDto>>("api/analysis/projects") ?? new(); }
        catch { return new(); }
    }

    public async Task<ProjectDto?> CreateProjectAsync(CreateProjectRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/analysis/project", req);
            return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<ProjectDto>() : null;
        }
        catch { return null; }
    }

    public async Task DeleteProjectAsync(int id)
    {
        try { await _http.DeleteAsync($"api/analysis/projects/{id}"); }
        catch { }
    }

    public async Task<MetricsDto?> GetMetricsAsync(int projectId)
    {
        try { return await _http.GetFromJsonAsync<MetricsDto>($"api/analysis/projects/{projectId}/metrics"); }
        catch { return null; }
    }

    public async Task<QualityGateDto?> GetQualityGateAsync(int projectId)
    {
        try { return await _http.GetFromJsonAsync<QualityGateDto>($"api/analysis/projects/{projectId}/quality-gate"); }
        catch { return null; }
    }

    public async Task<List<ScanDto>> GetScanHistoryAsync(int projectId)
    {
        try { return await _http.GetFromJsonAsync<List<ScanDto>>($"api/analysis/projects/{projectId}/scans") ?? new(); }
        catch { return new(); }
    }

    public async Task<ScanResultDto?> RunScanAsync(int projectId)
    {
        try
        {
            var res = await _http.PostAsync($"api/analysis/scan/{projectId}", null);
            return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<ScanResultDto>() : null;
        }
        catch { return null; }
    }

    public async Task<IssuePageDto?> GetIssuesAsync(
        int projectId,
        string? severity = null,
        string? category = null,
        string? status = null,
        string? search = null,
        int page = 1)
    {
        try
        {
            var qs = new List<string> { $"page={page}", "pageSize=50" };
            if (!string.IsNullOrEmpty(severity) && severity != "All") qs.Add($"severity={Uri.EscapeDataString(severity)}");
            if (!string.IsNullOrEmpty(category) && category != "All") qs.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrEmpty(status) && status != "All") qs.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrEmpty(search)) qs.Add($"search={Uri.EscapeDataString(search)}");

            return await _http.GetFromJsonAsync<IssuePageDto>(
                $"api/analysis/issues/{projectId}?{string.Join("&", qs)}");
        }
        catch { return null; }
    }

    public async Task UpdateIssueStatusAsync(int issueId, string status)
    {
        try { await _http.PatchAsJsonAsync($"api/analysis/issues/{issueId}/status", status); }
        catch { }
    }

    public async Task<List<RuleDto>> GetRulesAsync(string? category = null, string? search = null)
    {
        try
        {
            var qs = new List<string>();
            if (!string.IsNullOrEmpty(category) && category != "All") qs.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrEmpty(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
            var url = qs.Count > 0 ? $"api/analysis/rules?{string.Join("&", qs)}" : "api/analysis/rules";
            return await _http.GetFromJsonAsync<List<RuleDto>>(url) ?? new();
        }
        catch { return new(); }
    }

    public async Task UpdateRuleAsync(string ruleId, bool? enabled, string? severity)
    {
        try
        {
            await _http.PutAsJsonAsync($"api/analysis/rules/{Uri.EscapeDataString(ruleId)}",
                new { Enabled = enabled, Severity = severity });
        }
        catch { }
    }

    public async Task<DashboardSummaryDto?> GetDashboardAsync()
    {
        try { return await _http.GetFromJsonAsync<DashboardSummaryDto>("api/analysis/dashboard"); }
        catch { return null; }
    }
}
