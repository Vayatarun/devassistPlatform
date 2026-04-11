using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer1.Rules.Efficiency
{
    [RuleCategory("Efficiency")]

    public class AvoidCountInsteadOfAnyRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "EFF002",
            Title = "Use Any() instead of Count() for existence check",
            Description = "Using Count() to check if a collection has elements is inefficient. Use Any() instead.",
            Category = "Efficiency",
            DefaultSeverity = Severity.Critical,
           // Remediation = "Replace Count() > 0 with Any(), and Count() == 0 with !Any()"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var binaryExpressions = context.SyntaxRoot.DescendantNodes()
                .OfType<BinaryExpressionSyntax>();

            foreach (var binary in binaryExpressions)
            {
                // Look for patterns like: Count() > 0 or Count() == 0
                if (!(binary.Left is InvocationExpressionSyntax invocation))
                    continue;

                var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (methodSymbol == null)
                    continue;

                // Check method is Count()
                if (methodSymbol.Name != "Count")
                    continue;

                // Ensure it's LINQ Count() (extension method on IEnumerable)
                if (!IsLinqCount(methodSymbol))
                    continue;

                // Right side should be constant (0)
                var constant = context.SemanticModel.GetConstantValue(binary.Right);
                if (!constant.HasValue || !(constant.Value is int value))
                    continue;

                if (value != 0)
                    continue;

                // Check operator
                var operatorKind = binary.OperatorToken.ValueText;

                if (operatorKind == ">" || operatorKind == "==" || operatorKind == "!=")
                {
                    var suggestion = GetSuggestion(operatorKind);

                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Use Any() instead of Count() {operatorKind} 0. {suggestion}",
                        Line = binary.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Metadata.DefaultSeverity
                    });
                }
            }

            return issues;
        }

        private bool IsLinqCount(IMethodSymbol methodSymbol)
        {
            return methodSymbol.ContainingType != null &&
                   methodSymbol.ContainingType.Name == "Enumerable" &&
                   methodSymbol.ContainingNamespace.ToDisplayString().Contains("System.Linq");
        }

        private string GetSuggestion(string op)
        {
            switch (op)
            {
                case ">":
                    return "Replace with .Any()";
                case "==":
                    return "Replace with !.Any()";
                case "!=":
                    return "Replace with .Any()";
                default:
                    return "Use Any() for better performance";
            }
        }
    }
}
