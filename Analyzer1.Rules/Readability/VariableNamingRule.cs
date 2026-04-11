using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Analyzer.Rules.Naming
{
    [RuleCategory("Naming")]
    public class VariableNamingRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "NAME005",
            Title = "Variable naming convention",
            Category = "Naming",
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

                if (!Regex.IsMatch(name, "^[a-z][a-zA-Z0-9]*$"))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Variable '{name}' should be camelCase.",
                        Line = variable.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Minor
                    });
                }
            }

            return issues;
        }
    }
}