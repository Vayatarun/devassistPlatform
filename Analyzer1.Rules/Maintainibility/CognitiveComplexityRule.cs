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
           // Remediation = "Refactor method to reduce nesting and simplify logic."
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
                        Message = $"Method '{method.Identifier.Text}' has high cognitive complexity ({complexity}).",
                        Line = method.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = CalculateSeverity(complexity)
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