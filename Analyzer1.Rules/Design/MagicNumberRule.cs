using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Bug 6 fix: was in global namespace — RuleLoader.LoadRules filters by FullName.StartsWith("Analyzer")
namespace Analyzer1.Rules.Design
{
    [RuleCategory("Design")]
    public class MagicNumberRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "DES005",
            Title = "Magic number detected",
            DefaultSeverity = Severity.Info,
            Description = "Avoid using hardcoded numeric values",
            Category = "Design",
            Remediation = "Replace the literal with a named constant or configuration value that explains its purpose.",
            WhyItMatters = "Magic numbers are unexplained values embedded in code. '86400' means nothing without context, but 'const int SecondsPerDay = 86400' is self-documenting and centralizes the value for easy updates.",
            BadCodeExample =
                "// BAD: what do these numbers mean?\n" +
                "if (age > 18) { ... }        // Voting? Drinking? Driving?\n" +
                "Thread.Sleep(3000);           // Why 3000? Seconds? Milliseconds?\n" +
                "var buf = new byte[65536];    // Why 65536?",
            GoodCodeExample =
                "// GOOD: self-documenting named constants\n" +
                "const int MinimumVotingAge = 18;\n" +
                "const int RetryDelayMs = 3000;\n" +
                "const int BufferSize = 64 * 1024; // 64KB\n\n" +
                "if (age > MinimumVotingAge) { ... }\n" +
                "Thread.Sleep(RetryDelayMs);\n" +
                "var buf = new byte[BufferSize];"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            if (context?.SyntaxRoot == null)
                yield break;

            var literals = context.SyntaxRoot.DescendantNodes()
                .OfType<LiteralExpressionSyntax>();

            foreach (var literal in literals)
            {
                if (literal.Token.Value is int value && value != 0 && value != 1)
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Magic number '{value}' detected. Replace with a named constant that explains its meaning.",
                        Severity = Metadata.DefaultSeverity,
                        Line = literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        SuggestedFix = $"Replace '{value}' with a named constant: private const int MEANINGFUL_NAME = {value};",
                        WhyItMatters = Metadata.WhyItMatters,
                        BadCodeExample = Metadata.BadCodeExample,
                        GoodCodeExample = Metadata.GoodCodeExample
                    };
                }
            }
        }
    }
}
