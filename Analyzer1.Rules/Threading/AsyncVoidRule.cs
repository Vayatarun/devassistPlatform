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
    public class AsyncVoidRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "THR004",
            Title = "Avoid async void methods",
            Category = "Threading",
            DefaultSeverity = Severity.Major
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var methods = context.SyntaxRoot.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                if (method.Modifiers.Any(m => m.Text == "async") &&
                    method.ReturnType.ToString() == "void")
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Avoid async void. Use Task instead.",
                        Line = method.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Major
                    });
                }
            }

            return issues;
        }
    }
}