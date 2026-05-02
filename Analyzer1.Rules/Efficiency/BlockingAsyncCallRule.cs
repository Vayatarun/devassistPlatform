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
    public class BlockingAsyncCallRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "EFF004",
            Title = "Avoid blocking async calls",
            Description = "Blocking async calls using .Result, .Wait(), or GetAwaiter().GetResult() can cause deadlocks and thread starvation.",
            Category = "Efficiency",
            DefaultSeverity = Severity.Critical,
            Remediation = "Make the calling method async and use await instead of blocking. Propagate async all the way up the call stack.",
            WhyItMatters = "Blocking on async code in ASP.NET holds a thread-pool thread while waiting, starving the server. In some sync contexts it deadlocks: the waiting thread holds the sync context lock that the async continuation also needs.",
            BadCodeExample =
                "// BAD: blocks current thread — deadlock risk in ASP.NET!\n" +
                "public string GetUser(int id) {\n" +
                "    return _service.GetUserAsync(id).Result; // hangs under load\n" +
                "}",
            GoodCodeExample =
                "// GOOD: async all the way — thread is released while awaiting\n" +
                "public async Task<string> GetUser(int id) {\n" +
                "    return await _service.GetUserAsync(id);\n" +
                "}"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            // Bug 4 fix: SemanticModel may be null; use syntax-only path when it is
            var semanticModel = context.SemanticModel;

            var nodes = context.SyntaxRoot.DescendantNodes();

            foreach (var node in nodes)
            {
                // Detect .Result
                if (node is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text == "Result")
                {
                    if (semanticModel != null)
                    {
                        var symbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
                        if (IsTaskType(symbol))
                            issues.Add(CreateIssue(memberAccess, "Use 'await' instead of '.Result' to avoid thread-pool starvation and deadlocks."));
                    }
                    else if (memberAccess.ToString().Contains("Task") ||
                             memberAccess.Parent is not ObjectCreationExpressionSyntax)
                    {
                        issues.Add(CreateIssue(memberAccess, "Use 'await' instead of '.Result' to avoid thread-pool starvation and deadlocks."));
                    }
                }

                // Detect .Wait()
                if (node is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax waitAccess &&
                    waitAccess.Name.Identifier.Text == "Wait")
                {
                    if (semanticModel != null)
                    {
                        var symbol = semanticModel.GetSymbolInfo(waitAccess.Expression).Symbol;
                        if (IsTaskType(symbol))
                            issues.Add(CreateIssue(invocation, "Use await instead of .Wait()"));
                    }
                    else
                    {
                        issues.Add(CreateIssue(invocation, "Use await instead of .Wait()"));
                    }
                }

                // Detect GetAwaiter().GetResult()
                if (node is InvocationExpressionSyntax getResultInvocation &&
                    getResultInvocation.Expression is MemberAccessExpressionSyntax getResultAccess &&
                    getResultAccess.Name.Identifier.Text == "GetResult" &&
                    getResultAccess.Expression is InvocationExpressionSyntax innerInvocation &&
                    innerInvocation.Expression is MemberAccessExpressionSyntax awaiterAccess &&
                    awaiterAccess.Name.Identifier.Text == "GetAwaiter")
                {
                    if (semanticModel != null)
                    {
                        var symbol = semanticModel.GetSymbolInfo(awaiterAccess.Expression).Symbol;
                        if (IsTaskType(symbol))
                            issues.Add(CreateIssue(getResultInvocation, "Use await instead of GetAwaiter().GetResult()"));
                    }
                    else
                    {
                        issues.Add(CreateIssue(getResultInvocation, "Use await instead of GetAwaiter().GetResult()"));
                    }
                }
            }

            // Bug 10 fix: removed the second loop that re-scanned async methods for the same
            // .Result / .Wait() patterns — it caused every blocking call inside an async method
            // to be reported twice. The first loop above already covers all occurrences.

            return issues;
        }

        private bool IsTaskType(ISymbol symbol)
        {
            if (symbol == null)
                return false;

            var type = symbol switch
            {
                ILocalSymbol local => local.Type,
                IPropertySymbol prop => prop.Type,
                IMethodSymbol method => method.ReturnType,
                _ => null
            };

            if (type == null)
                return false;

            return type.Name == "Task" ||
                   type.Name == "ValueTask" ||
                   type.ToString().StartsWith("System.Threading.Tasks.Task");
        }

        private CodeIssue CreateIssue(SyntaxNode node, string message)
        {
            return new CodeIssue
            {
                RuleId = Metadata.RuleId,
                Message = message,
                Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Severity = Metadata.DefaultSeverity,
                SuggestedFix = "Make the method 'async Task<T>' and replace the blocking call with 'await'",
                WhyItMatters = Metadata.WhyItMatters,
                BadCodeExample = Metadata.BadCodeExample,
                GoodCodeExample = Metadata.GoodCodeExample
            };
        }
    }
}
