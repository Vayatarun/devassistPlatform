using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Analyzer1.Rules.Stability
{
    [RuleCategory("Stability")]
    public class PossibleNullReferenceRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "STAB001",
            Title = "Possible null reference usage",
            DefaultSeverity = Severity.Critical,
            Description = "Object is used without null check, may cause NullReferenceException",
            Category = "Stability Issue"
        };





        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var root = context.SyntaxRoot;
            var semanticModel = context.SemanticModel;

            var memberAccesses = root.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>();

            foreach (var memberAccess in memberAccesses)
            {
                var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);

                if (typeInfo.Type != null &&
                    typeInfo.Nullability.Annotation == NullableAnnotation.Annotated &&
                    typeInfo.Nullability.FlowState != NullableFlowState.NotNull)
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Possible null reference: {memberAccess.Expression}",
                        Severity = Metadata.DefaultSeverity,
                        Line = memberAccess.GetLocation()
                                           .GetLineSpan()
                                           .StartLinePosition.Line + 1
                    };
                }
            }
        }
    }
}
