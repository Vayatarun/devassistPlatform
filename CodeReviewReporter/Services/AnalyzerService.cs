using CodeReviewReporter.Models;
using CodeReviewReporter.Models.CodeReviewReporter.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace CodeReviewReporter.Services
{
    public class AnalyzerService
    {
        public async Task<List<CodeReviewIssue>> AnalyzeSolution(string solutionPath)
        {
            var issues = new List<CodeReviewIssue>();

            using var workspace = MSBuildWorkspace.Create();

            var solution = await workspace.OpenSolutionAsync(solutionPath);

            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();

                var diagnostics = compilation.GetDiagnostics();

                foreach (var diag in diagnostics)
                {
                    if (!diag.Location.IsInSource)
                        continue;

                    var span = diag.Location.GetLineSpan();

                    issues.Add(new CodeReviewIssue
                    {
                        FilePath = span.Path,
                        Line = span.StartLinePosition.Line + 1,
                        RuleId = diag.Id,
                        Category = diag.Descriptor.Category,
                        Message = diag.GetMessage(),
                        Severity = diag.Severity.ToString()
                    });
                }
            }

            return issues;
        }
    }
}