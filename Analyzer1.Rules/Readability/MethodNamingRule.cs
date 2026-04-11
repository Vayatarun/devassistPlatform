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
    public class MethodNamingRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "NAME002",
            Title = "Method naming convention",
            Category = "Naming",
            DefaultSeverity = Severity.Minor
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var methods = context.SyntaxRoot.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var name = method.Identifier.Text;

                if (!Regex.IsMatch(name, "^[A-Z][a-zA-Z0-9]*$"))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Method '{name}' should be PascalCase.",
                        Line = method.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Minor
                    });
                }
            }

            return issues;
        }
    }
}