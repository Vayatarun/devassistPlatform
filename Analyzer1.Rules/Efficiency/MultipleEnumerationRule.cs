using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer1.Rules.Efficiency
{
    [RuleCategory("Efficiency")]
    public class MultipleEnumerationRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "EFF001",
            Title = "Avoid multiple enumeration of IEnumerable",
            Description = "Enumerating IEnumerable multiple times can cause performance issues.",
            Category = "Efficiency",
            DefaultSeverity = Severity.Critical
           // Remediation = "Materialize using ToList() or ToArray()"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var invocations = context.SyntaxRoot.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            var grouped = invocations
                .GroupBy(i => i.Expression.ToString());

            foreach (var group in grouped)
            {
                if (group.Count() > 1)
                {
                    issues.Add(new CodeIssue
                    {
                        Message = "Possible multiple enumeration of IEnumerable.",
                        Line = group.First().GetLocation().GetLineSpan().StartLinePosition.Line
                    });
                }
            }

            return issues;
        }
    }
}
