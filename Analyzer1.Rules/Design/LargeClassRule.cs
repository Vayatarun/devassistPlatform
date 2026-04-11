using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

[RuleCategory("Design")]
public class LargeClassRule : IRule
{
    private const int MaxMembers = 20;

    public RuleMetadata Metadata => new RuleMetadata
    {
        RuleId = "DES004",
        Title = "Large class detected",
        DefaultSeverity = Severity.Critical,
        Description = "Class has too many members"
    };

    public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
    {
        var classes = context.SyntaxRoot.DescendantNodes()
            .OfType<ClassDeclarationSyntax>();

        foreach (var cls in classes)
        {
            int memberCount = cls.Members.Count;

            if (memberCount > MaxMembers)
            {
                yield return new CodeIssue
                {
                    RuleId = Metadata.RuleId,
                    Message = $"Class '{cls.Identifier.Text}' has {memberCount} members",
                    Severity = Metadata.DefaultSeverity,
                    Line = cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                };
            }
        }
    }
}