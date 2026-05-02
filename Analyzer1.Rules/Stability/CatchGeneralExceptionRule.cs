using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

// Bug 6 fix: was in global namespace — RuleLoader.LoadRules filters by FullName.StartsWith("Analyzer")
namespace Analyzer1.Rules.Stability
{
    [RuleCategory("Stability")]
    public class CatchGeneralExceptionRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "EX001",
            Title = "Avoid catching general Exception",
            Description = "Catching System.Exception hides specific exceptions.",
            DefaultSeverity = Severity.Warning,
            Category = "Exception Handling",
            Remediation = "Catch only the specific exception types you can handle. Use multiple catch blocks for different failure modes.",
            WhyItMatters = "Catching System.Exception masks the true failure: you handle OutOfMemoryException the same as IOException, losing critical diagnostic context and making safe recovery impossible.",
            BadCodeExample =
                "catch (Exception ex) {\n" +
                "    // Handles EVERYTHING — OutOfMemory, StackOverflow, corruption...\n" +
                "    _logger.LogError(ex, \"Something went wrong\");\n" +
                "    // You cannot safely continue after some of these!",
            GoodCodeExample =
                "catch (SqlException ex) when (ex.Number == -2) {\n" +
                "    _logger.LogWarning(\"DB timeout, retrying...\");\n" +
                "    await RetryAsync();\n" +
                "} catch (HttpRequestException ex) {\n" +
                "    _logger.LogError(ex, \"HTTP call failed\");\n" +
                "    throw; // propagate — caller decides\n" +
                "}"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var root = context.SyntaxRoot;

            // Bug 4 fix: SemanticModel is null when called without full compilation context.
            // Fall back to syntax-only detection when SemanticModel is unavailable.
            var semanticModel = context.SemanticModel;

            var catches = root.DescendantNodes().OfType<CatchClauseSyntax>();

            foreach (var catchClause in catches)
            {
                var type = catchClause.Declaration?.Type;
                if (type == null) continue;

                bool isGeneralException;

                if (semanticModel != null)
                {
                    var symbol = semanticModel.GetTypeInfo(type).Type;
                    isGeneralException = symbol?.ToString() == "System.Exception";
                }
                else
                {
                    // Syntax-only fallback
                    isGeneralException = type.ToString() == "Exception";
                }

                if (isGeneralException)
                    yield return CreateIssue(context, catchClause, "Avoid catching general Exception. Catch only the specific exception type(s) you can handle.");
            }
        }

        private CodeIssue CreateIssue(AnalysisContext context, SyntaxNode node, string message)
            => new()
            {
                FilePath = context.FilePath,
                Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Message = message,
                RuleId = Metadata.RuleId,
                Severity = Metadata.DefaultSeverity,
                SuggestedFix = "Replace 'catch (Exception)' with a specific type, e.g. 'catch (IOException ex)' or 'catch (SqlException ex)'",
                WhyItMatters = Metadata.WhyItMatters,
                BadCodeExample = Metadata.BadCodeExample,
                GoodCodeExample = Metadata.GoodCodeExample
            };
    }
}
