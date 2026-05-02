using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

// Bug 6 fix: was in global namespace — RuleLoader.LoadRules filters by FullName.StartsWith("Analyzer")
namespace Analyzer1.Rules.Stability
{
    [RuleCategory("Stability")]
    public class EmptyCatchRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "R001",
            Title = "Avoid empty catch blocks",
            Description = "Catch blocks should not be empty. Handle or log the exception.",
            DefaultSeverity = Severity.Warning,
            Category = "Code Smell",
            Remediation = "Log the exception and either handle it meaningfully or re-throw. Never silently swallow errors.",
            WhyItMatters = "Empty catch blocks silently swallow exceptions, hiding bugs and making failures invisible. Your code appears to work while actually failing — making production debugging nearly impossible.",
            BadCodeExample =
                "try {\n" +
                "    ProcessData();\n" +
                "} catch (Exception ex) {\n" +
                "    // Nothing — exception silently swallowed!\n" +
                "    // The caller never knows ProcessData() failed.\n" +
                "}",
            GoodCodeExample =
                "try {\n" +
                "    ProcessData();\n" +
                "} catch (Exception ex) {\n" +
                "    _logger.LogError(ex, \"ProcessData failed for input {Id}\", id);\n" +
                "    throw; // re-throw so callers can react\n" +
                "}"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            if (context?.SyntaxRoot == null)
                yield break;

            var catchBlocks = context.SyntaxRoot
                .DescendantNodes()
                .OfType<CatchClauseSyntax>()
                .Where(c => c.Block != null && !c.Block.Statements.Any());

            foreach (var catchBlock in catchBlocks)
            {
                var location = catchBlock.GetLocation().GetLineSpan();

                yield return new CodeIssue
                {
                    RuleId = Metadata.RuleId,
                    Message = "Empty catch block detected. Consider logging or handling the exception.",
                    FilePath = context.FilePath,
                    Line = location.StartLinePosition.Line + 1,
                    Severity = Metadata.DefaultSeverity,
                    SuggestedFix = "Add: _logger.LogError(ex, \"Error description\"); throw;",
                    WhyItMatters = Metadata.WhyItMatters,
                    BadCodeExample = Metadata.BadCodeExample,
                    GoodCodeExample = Metadata.GoodCodeExample
                };
            }
        }
    }
}
