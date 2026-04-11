using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer1.Rules.Stability
{
    [RuleCategory("Stability")]
    public class AsyncVoidMethodRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "STAB005",
            Title = "Avoid async void methods",
            DefaultSeverity = Severity.Critical,
            Description = "Async void methods can crash application"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var root = context.SyntaxRoot;

            var methods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                // Check async void
                if (method.Modifiers.Any(SyntaxKind.AsyncKeyword) &&
                    method.ReturnType is PredefinedTypeSyntax returnType &&
                    returnType.Keyword.IsKind(SyntaxKind.VoidKeyword))
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Async void method: {method.Identifier.Text}",
                        Severity = Metadata.DefaultSeverity,
                        Line = method.GetLocation()
                                     .GetLineSpan()
                                     .StartLinePosition.Line + 1
                    };
                }
            }
        }
    }
}
