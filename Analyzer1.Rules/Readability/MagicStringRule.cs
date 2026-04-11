using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Readability
{
    [RuleCategory("Naming")]
    public class MagicStringRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "READ006",
            Title = "Magic string detected",
            Category = "Readability",
            DefaultSeverity = Severity.Minor
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var literals = context.SyntaxRoot.DescendantNodes()
                .OfType<LiteralExpressionSyntax>()
                .Where(l => l.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression));

            foreach (var literal in literals)
            {
                var value = literal.Token.ValueText;

                if (value.Length > 3 && !value.Contains(" "))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Magic string '{value}' detected. Consider constant.",
                        Line = literal.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Minor
                    });
                }
            }

            return issues;
        }
    }
}