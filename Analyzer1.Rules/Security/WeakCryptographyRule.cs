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
    public class WeakCryptographyRule : IRule
    {
        private static readonly string[] WeakAlgos = { "MD5", "SHA1" };

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "SEC004",
            Title = "Weak cryptographic algorithm",
            Category = "Security",
            DefaultSeverity = Severity.Critical
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var identifiers = context.SyntaxRoot.DescendantNodes()
                .OfType<IdentifierNameSyntax>();

            foreach (var id in identifiers)
            {
                if (WeakAlgos.Contains(id.Identifier.Text))
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Weak crypto algorithm '{id.Identifier.Text}' detected.",
                        Line = id.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Critical
                    });
                }
            }

            return issues;
        }
    }
}