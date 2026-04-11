using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
[RuleCategory("Documentation")]

public class MissingSummaryTagRule : IRule
{
    public RuleMetadata Metadata => new RuleMetadata
    {
        RuleId = "DOC002",
        Title = "Missing <summary> in XML documentation",
        DefaultSeverity = Severity.Info,
        Description = "XML documentation should contain <summary> tag"
    };

    public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
    {
        var members = context.SyntaxRoot.DescendantNodes()
            .OfType<MemberDeclarationSyntax>();

        foreach (var member in members)
        {
            var trivia = member.GetLeadingTrivia()
                .FirstOrDefault(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineDocumentationCommentTrivia));

            if (trivia != default)
            {
                var text = trivia.ToFullString();

                if (!text.Contains("<summary>"))
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Missing <summary> tag for '{GetName(member)}'",
                        Severity = Metadata.DefaultSeverity,
                        Line = member.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    };
                }
            }
        }
    }

    private string GetName(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax m => m.Identifier.Text,
            ClassDeclarationSyntax c => c.Identifier.Text,
            _ => "member"
        };
    }
}