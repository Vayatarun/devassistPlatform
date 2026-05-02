using Analyzer.Core.Registry;
using Analyzer1.API.Models;
using Analyzer1.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAnalyzerService _analyzer;

    public AnalysisController(AppDbContext db, IAnalyzerService analyzer)
    {
        _db = db;
        _analyzer = analyzer;
    }

    // ─── Projects ─────────────────────────────────────────────────────────────

    [HttpPost("project")]
    public async Task<IActionResult> CreateProject([FromBody] Project project)
    {
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return Ok(project);
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects()
    {
        var projects = await _db.Projects.ToListAsync();

        var counts = await _db.Issues
            .Where(i => i.Status == "Open")
            .GroupBy(i => i.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var p in projects)
            p.IssueCount = counts.FirstOrDefault(x => x.ProjectId == p.Id)?.Count ?? 0;

        return Ok(projects);
    }

    [HttpDelete("projects/{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null) return NotFound();

        _db.Issues.RemoveRange(_db.Issues.Where(i => i.ProjectId == id));
        _db.Scans.RemoveRange(_db.Scans.Where(s => s.ProjectId == id));
        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return Ok();
    }

    // ─── Metrics ──────────────────────────────────────────────────────────────

    [HttpGet("projects/{id}/metrics")]
    public async Task<IActionResult> GetMetrics(int id)
    {
        var issues = await _db.Issues
            .Where(i => i.ProjectId == id && i.Status == "Open")
            .ToListAsync();

        return Ok(new
        {
            TotalIssues = issues.Count,
            Bugs = issues.Count(i => i.Category == "Stability" || i.Category == "Threading"),
            Vulnerabilities = issues.Count(i => i.Category == "Security"),
            CodeSmells = issues.Count(i =>
                i.Category == "Design" || i.Category == "Maintainability" ||
                i.Category == "Readability" || i.Category == "Efficiency"),
            CriticalCount = issues.Count(i => i.Severity == "Critical"),
            ErrorCount = issues.Count(i => i.Severity == "Error"),
            WarningCount = issues.Count(i => i.Severity == "Warning"),
            InfoCount = issues.Count(i => i.Severity == "Info"),
            TechnicalDebtMinutes = issues.Sum(i => i.EffortMinutes)
        });
    }

    // ─── Quality Gate ─────────────────────────────────────────────────────────

    [HttpGet("projects/{id}/quality-gate")]
    public async Task<IActionResult> GetQualityGate(int id)
    {
        var issues = await _db.Issues
            .Where(i => i.ProjectId == id && i.Status == "Open")
            .ToListAsync();

        var critical = issues.Count(i => i.Severity == "Critical");
        var errors = issues.Count(i => i.Severity == "Error");
        var totalDebt = issues.Sum(i => i.EffortMinutes);

        var reasons = new List<string>();
        if (critical > 5) reasons.Add($"Critical issues: {critical} (threshold: 5)");
        if (errors > 20) reasons.Add($"Error issues: {errors} (threshold: 20)");

        var passed = reasons.Count == 0;

        var project = await _db.Projects.FindAsync(id);
        if (project != null)
        {
            project.QualityGateStatus = passed ? "Passed" : "Failed";
            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            Passed = passed,
            Status = passed ? "Passed" : "Failed",
            FailureReasons = reasons,
            CriticalCount = critical,
            ErrorCount = errors,
            TotalIssues = issues.Count,
            TechnicalDebtMinutes = totalDebt
        });
    }

    // ─── Scan History ─────────────────────────────────────────────────────────

    [HttpGet("projects/{id}/scans")]
    public async Task<IActionResult> GetScanHistory(int id)
    {
        var scans = await _db.Scans
            .Where(s => s.ProjectId == id)
            .OrderByDescending(s => s.StartedAt)
            .Take(10)
            .ToListAsync();
        return Ok(scans);
    }

    // ─── Run Scan ─────────────────────────────────────────────────────────────

    [HttpPost("scan/{projectId}")]
    public async Task<IActionResult> Scan(int projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project == null) return NotFound();

        var scan = new AnalysisScan
        {
            ProjectId = projectId,
            StartedAt = DateTime.Now,
            Status = "Running"
        };
        _db.Scans.Add(scan);
        await _db.SaveChangesAsync();

        try
        {
            var issues = await _analyzer.AnalyzeAsync(project.Path, projectId);

            _db.Issues.RemoveRange(_db.Issues.Where(x => x.ProjectId == projectId));
            await _db.Issues.AddRangeAsync(issues);

            project.LastScan = DateTime.Now;

            var critical = issues.Count(i => i.Severity == "Critical");
            var errors = issues.Count(i => i.Severity == "Error");
            var warnings = issues.Count(i => i.Severity == "Warning");
            var debt = issues.Sum(i => i.EffortMinutes);

            scan.CompletedAt = DateTime.Now;
            scan.Status = "Completed";
            scan.TotalIssues = issues.Count;
            scan.CriticalCount = critical;
            scan.ErrorCount = errors;
            scan.WarningCount = warnings;
            scan.TechnicalDebtMinutes = debt;
            scan.QualityGatePassed = critical <= 5 && errors <= 20;
            project.QualityGateStatus = scan.QualityGatePassed ? "Passed" : "Failed";

            await _db.SaveChangesAsync();

            return Ok(new
            {
                Scan = scan,
                IssueCount = issues.Count,
                CriticalCount = critical,
                QualityGatePassed = scan.QualityGatePassed
            });
        }
        catch (Exception ex)
        {
            scan.Status = "Failed";
            scan.CompletedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    // ─── Issues ───────────────────────────────────────────────────────────────

    [HttpGet("issues/{projectId}")]
    public async Task<IActionResult> GetIssues(
        int projectId,
        [FromQuery] string? severity = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.Issues.Where(x => x.ProjectId == projectId).AsQueryable();

        if (!string.IsNullOrEmpty(severity) && severity != "All")
            query = query.Where(i => i.Severity == severity);
        if (!string.IsNullOrEmpty(category) && category != "All")
            query = query.Where(i => i.Category == category);
        if (!string.IsNullOrEmpty(status) && status != "All")
            query = query.Where(i => i.Status == status);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(i =>
                i.Message.Contains(search) ||
                i.Rule.Contains(search) ||
                i.FilePath.Contains(search));

        var total = await query.CountAsync();
        var issues = await query
            .OrderBy(i => i.Severity == "Critical" ? 0 :
                          i.Severity == "Error" ? 1 :
                          i.Severity == "Warning" ? 2 : 3)
            .ThenBy(i => i.FilePath)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { Total = total, Page = page, PageSize = pageSize, Issues = issues });
    }

    [HttpPatch("issues/{id}/status")]
    public async Task<IActionResult> UpdateIssueStatus(int id, [FromBody] string status)
    {
        var issue = await _db.Issues.FindAsync(id);
        if (issue == null) return NotFound();
        issue.Status = status;
        await _db.SaveChangesAsync();
        return Ok(issue);
    }

    // ─── Rules ────────────────────────────────────────────────────────────────

    [HttpGet("rules")]
    public async Task<IActionResult> GetRules(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null)
    {
        if (!await _db.Rules.AnyAsync())
            await SeedRulesAsync();

        var query = _db.Rules.AsQueryable();

        if (!string.IsNullOrEmpty(category) && category != "All")
            query = query.Where(r => r.Category == category);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(r =>
                r.Name.Contains(search) ||
                r.RuleId.Contains(search) ||
                (r.Description != null && r.Description.Contains(search)));

        var rules = await query.OrderBy(r => r.Category).ThenBy(r => r.RuleId).ToListAsync();
        return Ok(rules);
    }

    [HttpPut("rules/{ruleId}")]
    public async Task<IActionResult> UpdateRule(string ruleId, [FromBody] RuleUpdateRequest req)
    {
        if (!await _db.Rules.AnyAsync())
            await SeedRulesAsync();

        var rule = await _db.Rules.FirstOrDefaultAsync(r => r.RuleId == ruleId);
        if (rule == null) return NotFound();

        if (req.Enabled.HasValue) rule.Enabled = req.Enabled.Value;
        if (!string.IsNullOrEmpty(req.Severity)) rule.Severity = req.Severity;

        await _db.SaveChangesAsync();
        return Ok(rule);
    }

    // ─── Dashboard ────────────────────────────────────────────────────────────

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var allIssues = await _db.Issues.Where(i => i.Status == "Open").ToListAsync();
        var projects = await _db.Projects.ToListAsync();
        var recentScan = await _db.Scans
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            TotalProjects = projects.Count,
            TotalIssues = allIssues.Count,
            CriticalIssues = allIssues.Count(i => i.Severity == "Critical"),
            ErrorIssues = allIssues.Count(i => i.Severity == "Error"),
            WarningIssues = allIssues.Count(i => i.Severity == "Warning"),
            InfoIssues = allIssues.Count(i => i.Severity == "Info"),
            TechnicalDebtMinutes = allIssues.Sum(i => i.EffortMinutes),
            ProjectsPassed = projects.Count(p => p.QualityGateStatus == "Passed"),
            ProjectsFailed = projects.Count(p => p.QualityGateStatus == "Failed"),
            LastScanAt = recentScan?.CompletedAt,
            LastScanIssues = recentScan?.TotalIssues ?? 0,
            IssuesByCategory = allIssues
                .GroupBy(i => string.IsNullOrEmpty(i.Category) ? "Other" : i.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList()
        });
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task SeedRulesAsync()
    {
        try
        {
            var loadedRules = RuleLoader.LoadRules();
            var rules = loadedRules
                .Where(r => r?.Metadata != null)
                .Select(r => new Rule
                {
                    RuleId = r.Metadata.RuleId,
                    Name = r.Metadata.Title,
                    Category = r.Metadata.Category,
                    Severity = r.Metadata.DefaultSeverity.ToString(),
                    Enabled = true,
                    Description = r.Metadata.Description,
                    Remediation = r.Metadata.Remediation,
                    Tags = string.Join(", ", r.Metadata.Tags)
                })
                .ToList();

            if (rules.Count > 0)
            {
                _db.Rules.AddRange(rules);
                await _db.SaveChangesAsync();
            }
        }
        catch { /* Rules stay empty if loading fails */ }
    }
}
