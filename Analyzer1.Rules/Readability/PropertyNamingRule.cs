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
    public class PropertyNamingRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "NAME003",
            Title = "Property naming convention",
            Category = "Naming",
            DefaultSeverity = Severity.Minor
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var properties = context.SyntaxRoot.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>();

            foreach (var prop in properties)
            {
                var name = prop.Identifier.Text;

                if (!Regex.IsMatch(name, "^[A-Z][a-zA-Z0-9]*$"))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Property '{name}' should be PascalCase.",
                        Line = prop.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Minor
                    });
                }
            }

            return issues;
        }
    }
}