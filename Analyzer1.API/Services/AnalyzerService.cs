using Analyzer1.API.Models;

namespace Analyzer1.API.Services
{
    public class AnalyzerService : IAnalyzerService
    {
        public async Task<List<Issue>> AnalyzeAsync(string projectPath, int projectId)
        {
            // 🔥 Replace this with your real AnalyzerEngine
            await Task.Delay(1000);

            return new List<Issue>
        {
            new Issue
            {
                Rule = "NullCheckRule",
                Severity = "Critical",
                FilePath = "UserService.cs",
                Line = 25,
                Message = "Possible null reference",
                ProjectId = projectId
            },
            new Issue
            {
                Rule = "AsyncVoidRule",
                Severity = "Major",
                FilePath = "EmailService.cs",
                Line = 10,
                Message = "Avoid async void methods",
                ProjectId = projectId
            }
        };
        }
    }
}
