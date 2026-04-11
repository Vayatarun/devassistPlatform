using Analyzer.Core.Models;

namespace Analyzer.Core.Interfaces;

public interface IAnalyzerEngine
{
    Task<IReadOnlyList<CodeIssue>> AnalyzeProjectAsync(
        string projectPath,
        CancellationToken cancellationToken = default);
}