using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Threading
{
    [RuleCategory("Threading")]
    public class TaskResultWaitRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "THR003",
            Title = "Task.Result or Wait() detected",
            Category = "Threading",
            DefaultSeverity = Severity.Critical,
            Remediation = "Make the calling method async and use 'await' instead of blocking. Never mix sync and async code.",
            WhyItMatters = "Task.Result and Task.Wait() block the current thread synchronously. In ASP.NET this causes thread exhaustion under load and deadlocks when the sync context cannot be re-entered.",
            BadCodeExample =
                "// BAD: blocks thread, deadlock risk in ASP.NET\n" +
                "var data = FetchDataAsync().Result;   // Thread blocked!\n" +
                "UpdateAsync().Wait();                  // Thread blocked!",
            GoodCodeExample =
                "// GOOD: await releases the thread while waiting\n" +
                "var data = await FetchDataAsync();\n" +
                "await UpdateAsync();"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            // Bug 14 fix: text.Contains(".Result") was flagging any property named Result
            // (e.g. ValidationResult.Result, HttpResponseMessage.Result).
            // Use syntax checks: flag .Result only on MemberAccessExpression nodes, and
            // .Wait() only on InvocationExpression nodes, to reduce false positives.

            var nodes = context.SyntaxRoot.DescendantNodes();

            foreach (var node in nodes)
            {
                // Detect someExpression.Result (member access, not a method call)
                if (node is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text == "Result" &&
                    !memberAccess.Expression.ToString().EndsWith("Result") &&
                    memberAccess.Parent is not ObjectCreationExpressionSyntax)
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Avoid blocking on Task.Result — use 'await' to prevent deadlocks and thread starvation.",
                        Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Severity = Severity.Critical,
                        SuggestedFix = "Replace '.Result' with 'await' and make the method 'async Task<T>'",
                        WhyItMatters = Metadata.WhyItMatters,
                        BadCodeExample = Metadata.BadCodeExample,
                        GoodCodeExample = Metadata.GoodCodeExample
                    });
                }

                // Detect someExpression.Wait()
                if (node is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax waitAccess &&
                    waitAccess.Name.Identifier.Text == "Wait" &&
                    invocation.ArgumentList.Arguments.Count == 0)
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Avoid blocking on Task.Wait() — use 'await' to prevent deadlocks and thread starvation.",
                        Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Severity = Severity.Critical,
                        SuggestedFix = "Replace '.Wait()' with 'await' and make the method 'async Task'",
                        WhyItMatters = Metadata.WhyItMatters,
                        BadCodeExample = Metadata.BadCodeExample,
                        GoodCodeExample = Metadata.GoodCodeExample
                    });
                }
            }

            return issues;
        }
    }
}
