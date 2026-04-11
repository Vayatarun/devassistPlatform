using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Analyzer1.Rules.Stability
{
    [RuleCategory("Stability")]
    public class IndexOutOfRangeRiskRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "STAB006",
            Title = "Possible index out of range",
            DefaultSeverity = Severity.Critical,
            Description = "Array or collection index access without bounds checking"
        };

  

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var root = context.SyntaxRoot;
            var semanticModel = context.SemanticModel;

            var elementAccesses = root.DescendantNodes()
                .OfType<ElementAccessExpressionSyntax>();

            foreach (var elementAccess in elementAccesses)
            {
                var expression = elementAccess.Expression;

                // 🔹 1. NULL CHECK
                var typeInfo = semanticModel.GetTypeInfo(expression);

                bool isNullable =
                    typeInfo.Type != null &&
                    typeInfo.Nullability.Annotation == NullableAnnotation.Annotated &&
                    typeInfo.Nullability.FlowState != NullableFlowState.NotNull;

                // 🔹 2. BASIC SAFE PATTERN SKIP (if inside if condition)
                var isInsideIf = elementAccess.Ancestors()
                    .OfType<IfStatementSyntax>()
                    .Any();

                // 🔹 3. LOOP SAFE PATTERN (for i < arr.Length)
                var isInsideLoop = elementAccess.Ancestors()
                    .OfType<ForStatementSyntax>()
                    .Any();

                // 🔹 4. INDEX COUNT CHECK (basic detection)
                var argument = elementAccess.ArgumentList.Arguments.FirstOrDefault();

                bool hasIndex = argument != null;

                // 🔹 FINAL DECISION
                if (isInsideIf || isInsideLoop)
                    continue;

                if (isNullable || hasIndex)
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Possible null or unsafe index access: {elementAccess}",
                        Severity = Metadata.DefaultSeverity,
                        Line = elementAccess.GetLocation()
                                            .GetLineSpan()
                                            .StartLinePosition.Line + 1
                    };
                }
            }
        }
    }
}

