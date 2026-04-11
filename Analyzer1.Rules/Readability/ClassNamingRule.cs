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
    public class ClassNamingRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "NAME001",
            Title = "Class naming convention",
            Category = "Naming",
            DefaultSeverity = Severity.Minor
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var classes = context.SyntaxRoot.DescendantNodes()
                .OfType<ClassDeclarationSyntax>();

            foreach (var cls in classes)
            {
                var name = cls.Identifier.Text;

                if (!Regex.IsMatch(name, "^[A-Z][a-zA-Z0-9]*$"))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Class '{name}' should be PascalCase.",
                        Line = cls.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Minor
                    });
                }
            }

            return issues;
        }
    }
}