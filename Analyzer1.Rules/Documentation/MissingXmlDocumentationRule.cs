using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
[RuleCategory("Documentation")]

public class MissingXmlDocumentationRule : IRule
{
    public RuleMetadata Metadata => new RuleMetadata
    {
        RuleId = "DOC001",
        Title = "Missing XML documentation",
        DefaultSeverity = Severity.Info,
        Description = "Public members should have XML documentation comments"
    };

    public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
    {
        var root = context.SyntaxRoot;

        var members = root.DescendantNodes()
            .OfType<MemberDeclarationSyntax>();

        foreach (var member in members)
        {
            // Check if public
            var symbol = context.SemanticModel.GetDeclaredSymbol(member);

            //var modifiers = member.GetModifiers();
            if (symbol?.DeclaredAccessibility == Accessibility.Public)
                continue;

            // Check for XML documentation
            var hasDoc = member.GetLeadingTrivia()
                .Any(t => t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SingleLineDocumentationCommentTrivia));

            if (!hasDoc)
            {
                yield return new CodeIssue
                {
                    RuleId = Metadata.RuleId,
                    Message = $"Public member '{GetName(member)}' is missing XML documentation",
                    Severity = Metadata.DefaultSeverity,
                    Line = member.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                };
            }
        }
    }

    private string GetName(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax m => m.Identifier.Text,
            ClassDeclarationSyntax c => c.Identifier.Text,
            PropertyDeclarationSyntax p => p.Identifier.Text,
            _ => "member"
        };
    }
}