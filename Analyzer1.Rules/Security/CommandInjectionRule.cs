using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Analyzer.Rules.Attributes;
namespace Analyzer.Rules.Security
{
   
    [RuleCategory("Security")]
    public class CommandInjectionRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "SEC007",
            Title = "Command injection risk",
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

                if (text.Contains("process.start") || text.Contains("cmd.exe"))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Potential command injection detected.",
                        Line = inv.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Critical
                    });
                }
            }

            return issues;
        }
    }
}