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

    // ✅ Create Project
    [HttpPost("project")]
    public async Task<IActionResult> CreateProject(Project project)
    {
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return Ok(project);
    }

    // ✅ Get Projects
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects()
    {
        return Ok(await _db.Projects.ToListAsync());
    }

    // 🔥 Run Scan
    [HttpPost("scan/{projectId}")]
    public async Task<IActionResult> Scan(int projectId)
    {
        var project = await _db.Projects.FindAsync(projectId);
        if (project == null)
            return NotFound();

        var issues = await _analyzer.AnalyzeAsync(project.Path, projectId);

        // Remove old issues
        var oldIssues = _db.Issues.Where(x => x.ProjectId == projectId);
        _db.Issues.RemoveRange(oldIssues);

        await _db.Issues.AddRangeAsync(issues);

        project.LastScan = DateTime.Now;

        await _db.SaveChangesAsync();

        return Ok(issues);
    }

    // ✅ Get Issues
    [HttpGet("issues/{projectId}")]
    public async Task<IActionResult> GetIssues(int projectId)
    {
        var issues = await _db.Issues
            .Where(x => x.ProjectId == projectId)
            .ToListAsync();

        return Ok(issues);
    }

    // ✅ Rules
    [HttpGet("rules")]
    public async Task<IActionResult> GetRules()
    {
        return Ok(await _db.Rules.ToListAsync());
    }
}