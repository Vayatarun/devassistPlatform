using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

// Alias to avoid ambiguity: Roslyn has its own AnalysisContext in Diagnostics namespace
using CoreAnalysisContext = Analyzer.Core.Models.AnalysisContext;

namespace Analyzer.Rules.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    [RuleCategory("Design")]
    public class DeepNestingRule : IRule
    {
        private const int MaxDepth = 3;

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "DES003",
            Title = "Deep nesting detected",
            DefaultSeverity = Severity.Critical,
            Description = "Too many nested control structures reduce readability and increase cyclomatic complexity.",
            Category = "Design",
            Remediation = "Extract nested logic into private methods. Use early-return (guard clauses) to reduce nesting depth.",
            EffortMinutes = 25,
            Tags = new[] { "nesting", "complexity", "readability", "design" },
            WhyItMatters = "Each nesting level forces the reader to track additional state simultaneously. Beyond 3 levels, code becomes very hard to reason about, test independently, and modify safely.",
            BadCodeExample =
                "// BAD: 4+ levels of nesting — hard to follow\n" +
                "if (user != null) {\n" +
                "    if (user.IsActive) {\n" +
                "        if (user.HasPermission) {\n" +
                "            if (data != null) {\n" +
                "                ProcessData(data); // buried!\n" +
                "            }\n" +
                "        }\n" +
                "    }\n" +
                "}",
            GoodCodeExample =
                "// GOOD: guard clauses — happy path is obvious\n" +
                "if (user == null) return;\n" +
                "if (!user.IsActive) return;\n" +
                "if (!user.HasPermission) return;\n" +
                "if (data == null) return;\n" +
                "ProcessData(data); // clear, depth 0"
        };

        // Bug 2 fix: use alias so this satisfies IRule.Analyze(AnalysisContext) unambiguously
        public IEnumerable<CodeIssue> Analyze(CoreAnalysisContext context)
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
                        Message = $"Deep nesting detected (depth: {depth}, max: {MaxDepth}). Reduce by inverting conditions into guard clauses.",
                        Severity = Metadata.DefaultSeverity,
                        Line = block.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        SuggestedFix = "Use guard clauses (early return): if (condition) return; — invert each nested condition and exit early",
                        WhyItMatters = Metadata.WhyItMatters,
                        BadCodeExample = Metadata.BadCodeExample,
                        GoodCodeExample = Metadata.GoodCodeExample
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
