using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Efficiency
{
    [RuleCategory("Efficiency")]
    public class LinqInLoopRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "EFF003",
            Title = "Avoid LINQ queries inside loops",
            Description = "Using LINQ queries inside loops can lead to O(n²) performance issues. Consider precomputing or using a dictionary.",
            Category = "Efficiency",
            DefaultSeverity = Severity.Critical,
           // Remediation = "Move LINQ query outside loop or use Dictionary/Lookup for O(1) access."
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var loops = context.SyntaxRoot.DescendantNodes()
                .Where(n =>
                    n is ForEachStatementSyntax ||
                    n is ForStatementSyntax ||
                    n is WhileStatementSyntax);

            foreach (var loop in loops)
            {
                var loopBody = GetLoopBody(loop);
                if (loopBody == null)
                    continue;

                var invocationExpressions = loopBody.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>();

                foreach (var invocation in invocationExpressions)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (symbol == null)
                        continue;

                    if (!IsLinqMethod(symbol))
                        continue;

                    // 🚨 Detect heavy LINQ operations
                    if (!IsExpensiveLinqMethod(symbol.Name))
                        continue;

                    // 🔍 Detect if LINQ is executed on external collection (not loop variable)
                    if (!IsUsingExternalCollection(invocation, loop))
                        continue;

                    // 🔥 Extra: detect nested LINQ inside loop
                    bool isNested = invocation.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                        .Any(inner =>
                        {
                            var innerSymbol = context.SemanticModel.GetSymbolInfo(inner).Symbol as IMethodSymbol;
                            return innerSymbol != null && IsLinqMethod(innerSymbol);
                        });

                    var message = BuildMessage(symbol.Name, isNested);

                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = message,
                        Line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Metadata.DefaultSeverity
                    });
                }
            }

            return issues;
        }

        private SyntaxNode GetLoopBody(SyntaxNode loop)
        {
            return loop switch
            {
                ForEachStatementSyntax fe => fe.Statement,
                ForStatementSyntax fs => fs.Statement,
                WhileStatementSyntax ws => ws.Statement,
                _ => null
            };
        }

        private bool IsLinqMethod(IMethodSymbol symbol)
        {
            return symbol.ContainingType != null &&
                   symbol.ContainingType.Name == "Enumerable" &&
                   symbol.ContainingNamespace.ToDisplayString().Contains("System.Linq");
        }

        private bool IsExpensiveLinqMethod(string methodName)
        {
            // High-cost LINQ operations
            return new[]
            {
                "Where",
                "First",
                "FirstOrDefault",
                "Single",
                "SingleOrDefault",
                "Count",
                "Any",
                "OrderBy",
                "OrderByDescending",
                "GroupBy",
                "ToList",
                "ToArray"
            }.Contains(methodName);
        }

        private bool IsUsingExternalCollection(InvocationExpressionSyntax invocation, SyntaxNode loop)
        {
            // Get all identifiers inside invocation
            var identifiers = invocation.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(i => i.Identifier.Text)
                .ToHashSet();

            // Get loop variable
            string loopVariable = null;

            if (loop is ForEachStatementSyntax fe)
                loopVariable = fe.Identifier.Text;

            // If invocation uses identifiers other than loop variable → external collection
            return identifiers.Any(id => id != loopVariable);
        }

        private string BuildMessage(string methodName, bool isNested)
        {
            var baseMsg = $"LINQ method '{methodName}' used inside loop may cause performance issues.";

            if (isNested)
            {
                return baseMsg + " Nested LINQ detected — high risk of O(n²) or worse complexity.";
            }

            return baseMsg + " Consider using Dictionary or precomputing results outside the loop.";
        }
    }
}