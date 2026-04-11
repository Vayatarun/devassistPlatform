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
           // Remediation = "Use async/await instead of blocking calls."
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var nodes = context.SyntaxRoot.DescendantNodes();

            foreach (var node in nodes)
            {
                // 🔴 Detect .Result
                if (node is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text == "Result")
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
                    if (IsTaskType(symbol))
                    {
                        issues.Add(CreateIssue(memberAccess, "Use await instead of .Result"));
                    }
                }

                // 🔴 Detect .Wait()
                if (node is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax waitAccess &&
                    waitAccess.Name.Identifier.Text == "Wait")
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(waitAccess.Expression).Symbol;
                    if (IsTaskType(symbol))
                    {
                        issues.Add(CreateIssue(invocation, "Use await instead of .Wait()"));
                    }
                }

                // 🔴 Detect GetAwaiter().GetResult()
                if (node is InvocationExpressionSyntax getResultInvocation &&
                    getResultInvocation.Expression is MemberAccessExpressionSyntax getResultAccess &&
                    getResultAccess.Name.Identifier.Text == "GetResult")
                {
                    if (getResultAccess.Expression is InvocationExpressionSyntax innerInvocation &&
                        innerInvocation.Expression is MemberAccessExpressionSyntax awaiterAccess &&
                        awaiterAccess.Name.Identifier.Text == "GetAwaiter")
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(awaiterAccess.Expression).Symbol;
                        if (IsTaskType(symbol))
                        {
                            issues.Add(CreateIssue(getResultInvocation, "Use await instead of GetAwaiter().GetResult()"));
                        }
                    }
                }
            }

            // 🔥 Extra: detect blocking inside async methods (very critical)
            var methods = context.SyntaxRoot.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                if (!method.Modifiers.Any(m => m.Text == "async"))
                    continue;

                var body = method.Body;
                if (body == null)
                    continue;

                var blockingCalls = body.DescendantNodes()
                    .Where(n =>
                        n is InvocationExpressionSyntax inv &&
                        inv.ToString().Contains(".Wait()") ||
                        n is MemberAccessExpressionSyntax ma &&
                        ma.ToString().EndsWith(".Result"));

                foreach (var call in blockingCalls)
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Blocking async call inside async method detected — high risk of deadlock.",
                        Line = call.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Critical
                    });
                }
            }

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
                Line = node.GetLocation().GetLineSpan().StartLinePosition.Line,
                Severity = Metadata.DefaultSeverity
            };
        }
    }
}