using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer.Rules.Maintainability
{
    [RuleCategory("Maintainibility")]
    public class PrivateMethodNeverCalledRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "MAIN011",
            Title = "Private method is never called (dead code)",
            Description = "A private method is declared but never invoked within the class — dead code that increases maintenance burden.",
            Category = "Maintainability",
            DefaultSeverity = Severity.Warning,
            Remediation = "Remove the unused method if it is truly dead code. If it is needed in the future, consider keeping it only when it has a clear purpose.",
            EffortMinutes = 10,
            Tags = new[] { "dead-code", "unused", "maintainability" }
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            if (context.SyntaxRoot == null || context.SemanticModel == null)
                yield break;

            var classes = context.SyntaxRoot.DescendantNodes()
                .OfType<ClassDeclarationSyntax>();

            foreach (var cls in classes)
            {
                var privateMethods = cls.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Modifiers.Any(SyntaxKind.PrivateKeyword))
                    .ToList();

                if (!privateMethods.Any())
                    continue;

                var declaredSymbols = privateMethods
                    .Select(m => context.SemanticModel.GetDeclaredSymbol(m))
                    .Where(s => s != null)
                    .ToHashSet(SymbolEqualityComparer.Default);

                var calledSymbols = cls.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Select(inv => context.SemanticModel.GetSymbolInfo(inv).Symbol)
                    .Where(s => s != null)
                    .ToHashSet(SymbolEqualityComparer.Default);

                foreach (var method in privateMethods)
                {
                    var symbol = context.SemanticModel.GetDeclaredSymbol(method);
                    if (symbol == null) continue;

                    if (!calledSymbols.Contains(symbol))
                    {
                        yield return new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = $"Private method '{method.Identifier.Text}' is never called.",
                            Line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Severity = Metadata.DefaultSeverity,
                            Category = Metadata.Category,
                            Remediation = Metadata.Remediation,
                            EffortMinutes = Metadata.EffortMinutes
                        };
                    }
                }
            }
        }
    }
}
