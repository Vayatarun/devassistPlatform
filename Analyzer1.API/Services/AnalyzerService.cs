using Analyzer1.API.Models;

namespace Analyzer1.API.Services
{
    public class AnalyzerService : IAnalyzerService
    {
        private readonly RoslynAnalyzerService _roslyn = new();

        public async Task<List<Issue>> AnalyzeAsync(string projectPath, int projectId)
        {
            if (!Directory.Exists(projectPath))
                return new List<Issue>();

            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .ToList();

            var allIssues = new List<Issue>();
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

            var tasks = csFiles.Select(async file =>
            {
                await semaphore.WaitAsync();
                try
                {
                    string code = await File.ReadAllTextAsync(file);
                    var reviewIssues = await _roslyn.AnalyzeCode(file, code);

                    lock (allIssues)
                    {
                        allIssues.AddRange(reviewIssues.Select(ri => new Issue
                        {
                            Rule = ri.RuleId ?? string.Empty,
                            RuleTitle = ri.Description ?? ri.RuleId ?? string.Empty,
                            Category = ri.Category ?? string.Empty,
                            Severity = NormalizeSeverity(ri.Severity),
                            FilePath = ri.FileName ?? file,
                            Line = ri.LineNumber,
                            Message = ri.Message ?? string.Empty,
                            Remediation = ri.Description,
                            SuggestedFix = ri.SuggestedFix,
                            BadCodeExample = ri.BadCodeExample,
                            GoodCodeExample = ri.GoodCodeExample,
                            EffortMinutes = EstimateEffort(ri.Severity),
                            Status = "Open",
                            ProjectId = projectId,
                            DetectedAt = DateTime.Now
                        }));
                    }
                }
                catch { /* skip un-parseable files */ }
                finally { semaphore.Release(); }
            });

            await Task.WhenAll(tasks);
            return allIssues;
        }

        private static string NormalizeSeverity(string? severity) => severity?.ToLower() switch
        {
            "critical" => "Critical",
            "error" or "major" => "Error",
            "warning" or "minor" => "Warning",
            _ => "Info"
        };

        private static int EstimateEffort(string? severity) => severity?.ToLower() switch
        {
            "critical" => 60,
            "error" or "major" => 30,
            "warning" or "minor" => 10,
            _ => 5
        };
    }
}
