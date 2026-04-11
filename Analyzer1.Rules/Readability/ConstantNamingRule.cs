using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Analyzer.Rules.Naming
{
    [RuleCategory("Naming")]
    public class ConstantNamingRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "NAME006",
            Title = "Constant naming convention",
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
                if (!field.Modifiers.Any(SyntaxKind.ConstKeyword))
                    continue;

                foreach (var variable in field.Declaration.Variables)
                {
                    var name = variable.Identifier.Text;

                    if (!Regex.IsMatch(name, "^[A-Z0-9_]+$"))
                    {
                        issues.Add(new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = $"Constant '{name}' should be UPPER_CASE.",
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