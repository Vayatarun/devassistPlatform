using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer1.Rules.Stability
{
    [RuleCategory("Stability")]
    public class InfiniteLoopRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "STAB008",
            Title = "Potential infinite loop",
            DefaultSeverity = Severity.Critical,
            Description = "Loop without a clear exit condition may cause infinite execution"
        };
        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var root = context.SyntaxRoot;

            // WHILE LOOPS
            var whileLoops = root.DescendantNodes()
                .OfType<WhileStatementSyntax>();

            foreach (var loop in whileLoops)
            {
                if (loop.Condition is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.TrueLiteralExpression))
                {
                    if (!HasExit(loop.Statement))
                    {
                        yield return CreateIssue(loop, "while(true) loop without exit");
                    }
                }
            }

            // DO-WHILE LOOPS
            var doLoops = root.DescendantNodes()
                .OfType<DoStatementSyntax>();

            foreach (var loop in doLoops)
            {
                if (loop.Condition is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.TrueLiteralExpression))
                {
                    if (!HasExit(loop.Statement))
                    {
                        yield return CreateIssue(loop, "do-while(true) loop without exit");
                    }
                }
            }

            // FOR(;;) LOOPS
            var forLoops = root.DescendantNodes()
                .OfType<ForStatementSyntax>();

            foreach (var loop in forLoops)
            {
                if (loop.Condition == null) // for(;;)
                {
                    if (!HasExit(loop.Statement))
                    {
                        yield return CreateIssue(loop, "for(;;) loop without exit");
                    }
                }
            }
        }

        private bool HasExit(StatementSyntax statement)
        {
            return statement.DescendantNodes().Any(n =>
                n is BreakStatementSyntax ||
                n is ReturnStatementSyntax ||
                n is ThrowStatementSyntax);
        }

        private CodeIssue CreateIssue(SyntaxNode node, string message)
        {
            return new CodeIssue
            {
                RuleId = Metadata.RuleId,
                Message = message,
                Severity = Metadata.DefaultSeverity,
                Line = node.GetLocation()
                           .GetLineSpan()
                           .StartLinePosition.Line + 1
            };
        }

    }
}
