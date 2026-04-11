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
    public class LockOnPublicObjectRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "THR005",
            Title = "Lock on publicly accessible object",
            Category = "Threading",
            DefaultSeverity = Severity.Critical
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var locks = context.SyntaxRoot.DescendantNodes()
                .OfType<LockStatementSyntax>();

            foreach (var l in locks)
            {
                var expr = l.Expression.ToString();

                if (expr == "this" || expr.ToLower().Contains("typeof"))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Locking on public object can cause deadlocks.",
                        Line = l.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Critical
                    });
                }
            }

            return issues;
        }
    }
}