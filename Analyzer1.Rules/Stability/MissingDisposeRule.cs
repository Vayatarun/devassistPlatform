using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Analyzer1.Rules.Stability
{
    [RuleCategory("Stability")]
    public class MissingDisposeRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "STAB004",
            Title = "IDisposable object not disposed",
            DefaultSeverity = Severity.Critical,
            Description = "Objects implementing IDisposable should be disposed properly",
            Category = "Stability Issue"
        };

        public IEnumerable<CodeIssue> Analyze(SyntaxNode node, SemanticModel model)
        {
            if (node is ObjectCreationExpressionSyntax obj)
            {
                var type = model.GetTypeInfo(obj).Type;

                if (type != null && type.AllInterfaces.Any(i => i.Name == "IDisposable"))
                {
                    if (!(node.Parent is UsingStatementSyntax))
                    {
                        yield return new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = $"Disposable object '{type.Name}' is not wrapped in using",
                            Severity = Metadata.DefaultSeverity,
                            Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        };
                    }
                }
            }
        }

       

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var root = context.SyntaxRoot;
            var semanticModel = context.SemanticModel;

            var objectCreations = root.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>();

            foreach (var objectCreation in objectCreations)
            {
                var typeInfo = semanticModel.GetTypeInfo(objectCreation).Type;

                if (typeInfo == null)
                    continue;

                // Check if type implements IDisposable
                var isDisposable = typeInfo.AllInterfaces
                    .Any(i => i.ToString() == "System.IDisposable");

                if (!isDisposable)
                    continue;

                // Check if inside using statement OR using declaration
                bool isInsideUsing =
                    objectCreation.Ancestors().OfType<UsingStatementSyntax>().Any() ||
                    objectCreation.Ancestors().OfType<LocalDeclarationStatementSyntax>()
                        .Any(ld => ld.UsingKeyword != default);

                if (!isInsideUsing)
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Disposable object '{typeInfo.Name}' is not wrapped in using",
                        Severity = Metadata.DefaultSeverity,
                        Line = objectCreation.GetLocation()
                                             .GetLineSpan()
                                             .StartLinePosition.Line + 1
                    };
                }
            }
        }
    }
}
