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
    public class FieldNamingRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "NAME004",
            Title = "Field naming convention",
            Category = "Naming",
            DefaultSeverity = Severity.Minor
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var fields = context.SyntaxRoot.DescendantNodes()
                .OfType<FieldDeclarationSyntax>();

            foreach (var field in fields)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    var name = variable.Identifier.Text;

                    if (!Regex.IsMatch(name, "^_[a-z][a-zA-Z0-9]*$"))
                    {
                        issues.Add(new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = $"Field '{name}' should be _camelCase.",
                            Line = variable.GetLocation().GetLineSpan().StartLinePosition.Line,
                            Severity = Severity.Minor
                        });
                    }
                }
            }

            return issues;
        }
    }
}