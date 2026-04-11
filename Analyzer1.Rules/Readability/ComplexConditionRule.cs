using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Readability
{
    [RuleCategory("Naming")]
    public class ComplexConditionRule : IRule
    {
        private const int Threshold = 3;

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "READ002",
            Title = "Complex condition",
            Category = "Readability",
            DefaultSeverity = Severity.Major
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var ifs = context.SyntaxRoot.DescendantNodes()
                .OfType<IfStatementSyntax>();

            foreach (var stmt in ifs)
            {
                int count = stmt.Condition.DescendantNodes()
                    .Count(n => n.ToString() == "&&" || n.ToString() == "||");

                if (count > Threshold)
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Condition too complex ({count} logical operators).",
                        Line = stmt.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Major
                    });
                }
            }

            return issues;
        }
    }
}