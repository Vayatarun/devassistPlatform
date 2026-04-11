using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Security
{
    [RuleCategory("Security")]
    public class OpenRedirectRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "SEC006",
            Title = "Open redirect vulnerability",
            Category = "Security",
            DefaultSeverity = Severity.Major
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var invocations = context.SyntaxRoot.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var inv in invocations)
            {
                var text = inv.ToString().ToLower();

                if (text.Contains("redirect(") && text.Contains("url"))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Unvalidated redirect detected.",
                        Line = inv.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Major
                    });
                }
            }

            return issues;
        }
    }
}