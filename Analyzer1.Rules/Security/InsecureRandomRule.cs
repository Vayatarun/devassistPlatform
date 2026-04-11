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
   public class InsecureRandomRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "SEC003",
            Title = "Insecure random number generation",
            Category = "Security",
            DefaultSeverity = Severity.Major
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var creations = context.SyntaxRoot.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>();

            foreach (var creation in creations)
            {
                if (creation.Type.ToString() == "Random")
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Use cryptographic RNG instead of Random.",
                        Line = creation.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Severity.Major
                    });
                }
            }

            return issues;
        }
    }
}