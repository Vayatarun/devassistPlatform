using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Readability
{
    [RuleCategory("Naming")]
    public class DeepNestingReadabilityRule : IRule
    {
        private const int MaxDepth = 3;

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "READ003",
            Title = "Deep nesting reduces readability",
            Category = "Readability",
            DefaultSeverity = Severity.Major
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var methods = context.SyntaxRoot.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                int depth = GetDepth(method.Body, 0);

                if (depth > MaxDepth)
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Deep nesting ({depth}) in method '{method.Identifier.Text}'.",
                        Line = method.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Major
                    });
                }
            }

            return issues;
        }

        private int GetDepth(SyntaxNode node, int level)
        {
            int max = level;

            foreach (var child in node.ChildNodes())
            {
                if (child is IfStatementSyntax ||
                    child is ForStatementSyntax ||
                    child is WhileStatementSyntax)
                {
                    max = System.Math.Max(max, GetDepth(child, level + 1));
                }
                else
                {
                    max = System.Math.Max(max, GetDepth(child, level));
                }
            }

            return max;
        }
    }
}