using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Security
{
    [RuleCategory("Security")]
    public class HardcodedCredentialRule : IRule
    {
        private static readonly string[] Keywords =
            { "password", "pwd", "secret", "apikey", "token" };

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "SEC002",
            Title = "Hardcoded credentials detected",
            Category = "Security",
            DefaultSeverity = Severity.Critical
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var variables = context.SyntaxRoot.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>();

            foreach (var variable in variables)
            {
                var name = variable.Identifier.Text.ToLower();

                if (!Keywords.Any(k => name.Contains(k)))
                    continue;

                var initializer = variable.Initializer?.Value?.ToString();

                if (!string.IsNullOrEmpty(initializer) && initializer.Contains("\""))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Hardcoded credential in '{variable.Identifier.Text}'.",
                        Line = variable.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Critical
                    });
                }
            }

            return issues;
        }
    }
}