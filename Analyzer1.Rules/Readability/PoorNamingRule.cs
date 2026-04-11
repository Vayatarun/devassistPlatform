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
    public class PoorNamingRule : IRule
    {
        private static readonly string[] BadNames = { "x", "y", "data", "temp", "obj", "val" };

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "READ004",
            Title = "Poor variable naming",
            Category = "Readability",
            DefaultSeverity = Severity.Minor
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var variables = context.SyntaxRoot.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>();

            foreach (var variable in variables)
            {
                var name = variable.Identifier.Text;

                if (name.Length <= 2 || BadNames.Contains(name.ToLower()))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Variable name '{name}' is not meaningful.",
                        Line = variable.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Minor
                    });
                }
            }

            return issues;
        }
    }
}