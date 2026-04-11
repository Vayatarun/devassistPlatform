using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Security
{
    [RuleCategory("Security")]
    public class SqlInjectionRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "SEC001",
            Title = "Potential SQL Injection vulnerability",
            Description = "SQL query constructed using string concatenation or interpolation.",
            Category = "Security",
            DefaultSeverity = Severity.Critical,
            //Remediation = "Use parameterized queries or ORM methods."
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            if (context.SyntaxRoot == null)
                return issues;

            // 🔹 Detect string concatenation
            var binaryExpressions = context.SyntaxRoot.DescendantNodes()
                .OfType<BinaryExpressionSyntax>()
                .Where(b => b.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddExpression));

            foreach (var expr in binaryExpressions)
            {
                var text = expr.ToString().ToLower();

                if (ContainsSqlKeyword(text) && ContainsVariable(expr))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Possible SQL Injection via string concatenation.",
                        Line = expr.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Critical
                    });
                }
            }

            // 🔹 Detect string interpolation
            var interpolations = context.SyntaxRoot.DescendantNodes()
                .OfType<InterpolatedStringExpressionSyntax>();

            foreach (var interp in interpolations)
            {
                var text = interp.ToString().ToLower();

                if (ContainsSqlKeyword(text))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Possible SQL Injection via string interpolation.",
                        Line = interp.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Critical
                    });
                }
            }

            return issues;
        }

        private bool ContainsSqlKeyword(string text)
        {
            return text.Contains("select") ||
                   text.Contains("insert") ||
                   text.Contains("update") ||
                   text.Contains("delete");
        }

        private bool ContainsVariable(BinaryExpressionSyntax expr)
        {
            return expr.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Any();
        }
    }
}