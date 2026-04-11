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

[RuleCategory("Stability")]
public class CatchGeneralExceptionRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "EX001",
            Title = "Avoid catching general Exception",
            Description = "Catching System.Exception hides specific exceptions.",
            DefaultSeverity = Severity.Warning,
            Category = "Exception Handling"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var root = context.SyntaxRoot;
            var semanticModel = context.SemanticModel;

            var catches = root.DescendantNodes()
                .OfType<CatchClauseSyntax>();

            foreach (var catchClause in catches)
            {
                var type = catchClause.Declaration?.Type;
                if (type == null) continue;

                var symbol = semanticModel.GetTypeInfo(type).Type;

                if (symbol?.ToString() == "System.Exception")
                {
                    yield return CreateIssue(context, catchClause, "Avoid catching general Exception.");
                }
            }
        }

        private CodeIssue CreateIssue(AnalysisContext context, SyntaxNode node, string message)
            => new()
            {
                FilePath = context.FilePath,
                Line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                Message = message,
                RuleId = Metadata.RuleId,
                Severity = Metadata.DefaultSeverity
            };
    }

