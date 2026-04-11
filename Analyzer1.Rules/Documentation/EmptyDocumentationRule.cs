using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

[RuleCategory("Documentation")]
public class EmptyDocumentationRule : IRule
{
  
    public RuleMetadata Metadata => new RuleMetadata
    {
        RuleId = "DOC004",
        Title = "Empty documentation",
        DefaultSeverity = Severity.Info,
        Description = "Documentation should not be empty"
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

                if (string.IsNullOrWhiteSpace(text.Replace("///", "").Trim()))
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Empty documentation for '{GetName(member)}'",
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