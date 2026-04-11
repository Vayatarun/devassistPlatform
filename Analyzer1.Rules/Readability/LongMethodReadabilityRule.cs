using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Readability
{
    [RuleCategory("Naming")]
    public class LongMethodReadabilityRule : IRule
    {
        private const int MaxLines = 50;

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "READ005",
            Title = "Method too long",
            Category = "Readability",
            DefaultSeverity = Severity.Major
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var methods = context.SyntaxRoot.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var span = method.GetLocation().GetLineSpan();
                int lines = span.EndLinePosition.Line - span.StartLinePosition.Line;

                if (lines > MaxLines)
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Method '{method.Identifier.Text}' is too long ({lines} lines).",
                        Line = span.StartLinePosition.Line,
                        Severity = Severity.Major
                    });
                }
            }

            return issues;
        }
    }
}