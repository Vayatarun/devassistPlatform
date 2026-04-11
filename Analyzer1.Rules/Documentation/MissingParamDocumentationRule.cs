using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

[RuleCategory("Documentation")]
public class MissingParamDocumentationRule : IRule
{
    public RuleMetadata Metadata => new RuleMetadata
    {
        RuleId = "DOC003",
        Title = "Missing parameter documentation",
        DefaultSeverity = Severity.Info,
        Description = "All parameters should be documented using <param>"
    };

    public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
    {
        var methods = context.SyntaxRoot.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var docTrivia = method.GetLeadingTrivia()
                .FirstOrDefault(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineDocumentationCommentTrivia));

            if (docTrivia == default)
                continue;

            var docText = docTrivia.ToFullString();

            foreach (var param in method.ParameterList.Parameters)
            {
                if (!docText.Contains($"param name=\"{param.Identifier.Text}\""))
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Parameter '{param.Identifier.Text}' is not documented in method '{method.Identifier.Text}'",
                        Severity = Metadata.DefaultSeverity,
                        Line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    };
                }
            }
        }
    }
}