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
    public class TaskResultWaitRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "THR003",
            Title = "Task.Result or Wait detected",
            Category = "Threading",
            DefaultSeverity = Severity.Critical
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var invocations = context.SyntaxRoot.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var inv in invocations)
            {
                var text = inv.ToString();

                if (text.Contains(".Wait(") || text.Contains(".Result"))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Avoid blocking async code. Use await.",
                        Line = inv.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Critical
                    });
                }
            }

            return issues;
        }
    }
}