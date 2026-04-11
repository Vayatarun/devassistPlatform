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
    public class SensitiveDataLoggingRule : IRule
    {
        private static readonly string[] Sensitive =
            { "password", "token", "secret" };

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "SEC005",
            Title = "Sensitive data logged",
            Category = "Security",
            DefaultSeverity = Severity.Critical
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var invocations = context.SyntaxRoot.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var inv in invocations)
            {
                var text = inv.ToString().ToLower();

                if (text.Contains("log") && Sensitive.Any(s => text.Contains(s)))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Sensitive data should not be logged.",
                        Line = inv.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Critical
                    });
                }
            }

            return issues;
        }
    }
}