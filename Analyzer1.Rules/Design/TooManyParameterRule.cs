using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Analyzer1.Rules.Design
{
  

    [RuleCategory("Design")]
    public class TooManyParametersRule : IRule
    {
        private const int MaxParameters = 5;

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "DES001",
            Title = "Too many parameters",
            DefaultSeverity = Severity.Critical,
            Description = "Methods should not have too many parameters"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var methods = context.SyntaxRoot.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                int count = method.ParameterList.Parameters.Count;

                if (count > MaxParameters)
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Method '{method.Identifier.Text}' has {count} parameters",
                        Severity = Metadata.DefaultSeverity,
                        Line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    };
                }
            }
        }
    }
}
