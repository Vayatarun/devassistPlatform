using Analyzer.Core.Configuration;
using Analyzer.Core.Security;
using Microsoft.CodeAnalysis;

namespace Analyzer.Core.Models;

public class AnalysisContext
{
    public string FilePath { get; init; } = string.Empty;
    public string SourceCode { get; init; } = string.Empty;

    // Language-specific metadata (Roslyn, JS parser etc.)
    public SyntaxNode? SyntaxRoot { get; init; }
    public SemanticModel? SemanticModel { get; init; }

    public TaintTrackingEngine TaintEngine { get; set; }

    public Dictionary<string, object> Properties { get; } = new();
    public GlobalAnalysisStore GlobalStore { get; set; }
    public RuleConfig Config { get; set; }

}