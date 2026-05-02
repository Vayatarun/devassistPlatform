using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Analyzer.Rules.Design
{
    [RuleCategory("Design")]
    public class ShotgunSurgeryCollectorRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "DESIGN004_COLLECT",
            Title = "Shotgun Surgery Collector",
            Category = "Design",
            DefaultSeverity = Severity.Info
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            if (context.SyntaxRoot == null || context.SemanticModel == null)
                return Enumerable.Empty<CodeIssue>();

            var invocations = context.SyntaxRoot.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (symbol == null)
                    continue;

                var methodKey = $"{symbol.ContainingType}.{symbol.Name}";

                var classNode = invocation.Ancestors()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault();

                // Bug 13 fix: MethodUsageMap is now ConcurrentDictionary<string, ConcurrentBag<>>
                var bag = context.GlobalStore.MethodUsageMap.GetOrAdd(
                    methodKey, _ => new System.Collections.Concurrent.ConcurrentBag<CallSiteInfo>());

                bag.Add(new CallSiteInfo
                {
                    FilePath = context.FilePath,
                    ClassName = classNode?.Identifier.Text ?? "Unknown",
                    // Bug 9 fix: store 1-based line numbers
                    Line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }

            return Enumerable.Empty<CodeIssue>();
        }
    }
}
