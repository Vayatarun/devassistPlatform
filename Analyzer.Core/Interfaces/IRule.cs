using Analyzer.Core.Models;

namespace Analyzer.Core.Interfaces;

public interface IRule
{
    RuleMetadata Metadata { get; }

    IEnumerable<CodeIssue> Analyze(AnalysisContext context);
}