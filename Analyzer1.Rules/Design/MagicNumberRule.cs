using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

[RuleCategory("Design")]
public class MagicNumberRule : IRule
{
    public RuleMetadata Metadata => new RuleMetadata
    {
        RuleId = "DES005",
        Title = "Magic number detected",
        DefaultSeverity = Severity.Info,
        Description = "Avoid using hardcoded numeric values"
    };

    public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
    {
        var literals = context.SyntaxRoot.DescendantNodes()
            .OfType<LiteralExpressionSyntax>();

        foreach (var literal in literals)
        {
            if (literal.Token.Value is int value && value != 0 && value != 1)
            {
                yield return new CodeIssue
                {
                    RuleId = Metadata.RuleId,
                    Message = $"Magic number detected: {value}",
                    Severity = Metadata.DefaultSeverity,
                    Line = literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                };
            }
        }
    }
}