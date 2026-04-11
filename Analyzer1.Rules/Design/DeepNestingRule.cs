using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Analyzer.Rules.Attributes;
namespace Analyzer.Rules.Design
{
    [RuleCategory("Design")]
    public class DeepNestingRule : IRule
    {
        private const int MaxDepth = 3;

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "DES003",
            Title = "Deep nesting detected",
            DefaultSeverity = Severity.Critical,
            Description = "Too many nested control structures"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var blocks = context.SyntaxRoot.DescendantNodes()
                .OfType<BlockSyntax>();

            foreach (var block in blocks)
            {
                int depth = GetDepth(block);

                if (depth > MaxDepth)
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Deep nesting detected (depth: {depth})",
                        Severity = Metadata.DefaultSeverity,
                        Line = block.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    };
                }
            }
        }

        private int GetDepth(SyntaxNode node)
        {
            int depth = 0;
            var current = node.Parent;

            while (current != null)
            {
                if (current is IfStatementSyntax ||
                    current is ForStatementSyntax ||
                    current is WhileStatementSyntax)
                {
                    depth++;
                }
                current = current.Parent;
            }

            return depth;
        }
    }
}