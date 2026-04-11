using Analyzer1.API.Models;

namespace Analyzer1.API.Services
{
    public interface IAnalyzerService
    {
        Task<List<Issue>> AnalyzeAsync(string projectPath, int projectId);
    }
}
