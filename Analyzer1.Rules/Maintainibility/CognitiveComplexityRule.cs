using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Maintainability
{
    [RuleCategory("Maintainibility")]
    public class CognitiveComplexityRule : IRule
    {
        private const int Threshold = 15;

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "MAIN003",
            Title = "High cognitive complexity",
            Description = "Method is too complex to understand easily.",
            Category = "Maintainability",
            DefaultSeverity = Severity.Info,
            Remediation = "Refactor the method: extract sub-logic into smaller private methods, reduce nesting with guard clauses, and simplify conditional chains.",
            EffortMinutes = 45,
            Tags = new[] { "complexity", "maintainability", "cognitive-complexity" },
            WhyItMatters = "High cognitive complexity means it takes extraordinary mental effort to understand what a method does. It's hard to test all paths, bugs hide in the complexity, and modifications frequently introduce regressions.",
            BadCodeExample =
                "// BAD: deep nesting + many conditions = high cognitive complexity\n" +
                "void ProcessOrder(Order o) {\n" +
                "    if (o != null) {\n" +
                "        if (o.IsValid) {\n" +
                "            for (var item in o.Items) {\n" +
                "                if (item.InStock) {\n" +
                "                    if (item.Price > 0) { Ship(item); }\n" +
                "                }\n" +
                "            }\n" +
                "        }\n" +
                "    }\n" +
                "}",
            GoodCodeExample =
                "// GOOD: decomposed into small, testable methods\n" +
                "void ProcessOrder(Order o) {\n" +
                "    if (!IsOrderValid(o)) return;\n" +
                "    ShipEligibleItems(o.Items);\n" +
                "}\n" +
                "bool IsOrderValid(Order o) => o != null && o.IsValid;\n" +
                "void ShipEligibleItems(IEnumerable<Item> items) {\n" +
                "    foreach (var item in items.Where(i => i.InStock && i.Price > 0))\n" +
                "        Ship(item);\n" +
                "}"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            if (context.SyntaxRoot == null)
                return issues;

            var methods = context.SyntaxRoot.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Body != null);

            foreach (var method in methods)
            {
                int complexity = CalculateComplexity(method.Body, 0);

                if (complexity > Threshold)
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Method '{method.Identifier.Text}' has high cognitive complexity ({complexity}, threshold: {Threshold}). Extract logic into smaller private methods.",
                        Line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Severity = CalculateSeverity(complexity),
                        SuggestedFix = $"Extract sub-logic from '{method.Identifier.Text}' into private helper methods, and use guard clauses to reduce nesting",
                        WhyItMatters = Metadata.WhyItMatters,
                        BadCodeExample = Metadata.BadCodeExample,
                        GoodCodeExample = Metadata.GoodCodeExample
                    });
                }
            }

            return issues;
        }

        private int CalculateComplexity(SyntaxNode node, int nestingLevel)
        {
            int complexity = 0;

            foreach (var child in node.ChildNodes())
            {
                if (IsControlStructure(child))
                {
                    // +1 for control structure
                    complexity += 1;

                    // + nesting penalty
                    complexity += nestingLevel;

                    // recurse with increased nesting
                    complexity += CalculateComplexity(child, nestingLevel + 1);
                }
                else if (IsFlowBreak(child))
                {
                    complexity += 1;
                    complexity += CalculateComplexity(child, nestingLevel);
                }
                else
                {
                    complexity += CalculateComplexity(child, nestingLevel);
                }
            }

            return complexity;
        }

        private bool IsControlStructure(SyntaxNode node)
        {
            return node is IfStatementSyntax ||
                   node is ForStatementSyntax ||
                   node is ForEachStatementSyntax ||
                   node is WhileStatementSyntax ||
                   node is DoStatementSyntax ||
                   node is SwitchStatementSyntax ||
                   node is CatchClauseSyntax;
        }

        private bool IsFlowBreak(SyntaxNode node)
        {
            return node is BreakStatementSyntax ||
                   node is ContinueStatementSyntax ||
                   node is GotoStatementSyntax ||
                   node is ReturnStatementSyntax;
        }

        private Severity CalculateSeverity(int complexity)
        {
            if (complexity > 30)
                return Severity.Critical;

            if (complexity > 20)
                return Severity.Error;

            return Severity.Info;
        }
    }
}