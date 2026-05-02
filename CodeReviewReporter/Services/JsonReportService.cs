using CodeReviewReporter.Models.CodeReviewReporter.Models;
using System.Text.Json;

namespace CodeReviewReporter.Services
{
    public class JsonReportService
    {
        public void GenerateReport(List<CodeReviewIssue> issues, string outputPath)
        {
            var report = new
            {
                GeneratedAt = DateTime.UtcNow,
                TotalIssues = issues.Count,
                Summary = new
                {
                    Critical = issues.Count(i => i.Severity == "Critical"),
                    Error = issues.Count(i => i.Severity == "Error"),
                    Major = issues.Count(i => i.Severity == "Major"),
                    Warning = issues.Count(i => i.Severity == "Warning"),
                    Minor = issues.Count(i => i.Severity == "Minor"),
                    Info = issues.Count(i => i.Severity == "Info"),
                },
                ByCategory = issues
                    .GroupBy(i => string.IsNullOrEmpty(i.Category) ? "General" : i.Category)
                    .ToDictionary(g => g.Key, g => g.Count()),
                Issues = issues.Select(i => new
                {
                    i.FileName,
                    i.LineNumber,
                    i.RuleId,
                    i.Category,
                    i.Severity,
                    i.Message,
                    i.Code,
                    i.SuggestedFix
                })
            };

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(outputPath, json);
            Console.WriteLine($"JSON report generated: {Path.GetFullPath(outputPath)}");
        }
    }
}
