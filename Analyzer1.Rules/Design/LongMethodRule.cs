using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

[RuleCategory("Design")]
public class LongMethodRule : IRule
{
    private const int MaxLines = 50;

    public RuleMetadata Metadata => new RuleMetadata
    {
        RuleId = "DES002",
        Title = "Method too long",
        DefaultSeverity = Severity.Critical,
        Description = "Methods should not be too long"
    };

    public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
    {
        var methods = context.SyntaxRoot.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var span = method.GetLocation().GetLineSpan();
            int lines = span.EndLinePosition.Line - span.StartLinePosition.Line;

            if (lines > MaxLines)
            {
                yield return new CodeIssue
                {
                    RuleId = Metadata.RuleId,
                    Message = $"Method '{method.Identifier.Text}' is too long ({lines} lines)",
                    Severity = Metadata.DefaultSeverity,
                    Line = span.StartLinePosition.Line + 1
                };
            }
        }
    }
}