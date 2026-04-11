using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Threading
{
    [RuleCategory("Threading")]
    public class ThreadSleepRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "THR002",
            Title = "Thread.Sleep usage",
            Category = "Threading",
            DefaultSeverity = Severity.Major
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var invocations = context.SyntaxRoot.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var inv in invocations)
            {
                if (inv.ToString().Contains("Thread.Sleep"))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Avoid Thread.Sleep. Use async delay instead.",
                        Line = inv.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Major
                    });
                }
            }

            return issues;
        }
    }
}