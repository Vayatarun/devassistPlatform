using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Efficiency
{
    [RuleCategory("Efficiency")]
    public class StringConcatInLoopRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "EFF005",
            Title = "Avoid string concatenation inside loops",
            Description = "String concatenation inside loops creates excessive allocations. Use StringBuilder instead.",
            Category = "Efficiency",
            DefaultSeverity = Severity.Critical,
          //  Remediation = "Use StringBuilder for repeated string operations inside loops."
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var loops = context.SyntaxRoot.DescendantNodes()
                .Where(n =>
                    n is ForStatementSyntax ||
                    n is ForEachStatementSyntax ||
                    n is WhileStatementSyntax);

            foreach (var loop in loops)
            {
                var loopBody = GetLoopBody(loop);
                if (loopBody == null)
                    continue;

                // Track string variables declared outside loop but modified inside
                var stringVars = GetStringVariables(loop, context);

                // Detect patterns
                var concatAssignments = loopBody.DescendantNodes()
                    .OfType<AssignmentExpressionSyntax>()
                    .Where(a => a.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddAssignmentExpression));

                foreach (var assignment in concatAssignments)
                {
                    var leftSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;

                    if (leftSymbol == null || !stringVars.Contains(leftSymbol))
                        continue;

                    var severity = IsInsideNestedLoop(loop) ? Severity.Critical : Metadata.DefaultSeverity;

                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "String concatenation using '+=' inside loop detected. This causes excessive allocations.",
                        Line = assignment.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = severity
                    });
                }

                // 🔥 Detect string.Concat inside loop
                var invocations = loopBody.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>();

                foreach (var invocation in invocations)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    if (symbol == null)
                        continue;

                    if (symbol.ContainingType?.SpecialType == SpecialType.System_String &&
                        symbol.Name == "Concat")
                    {
                        issues.Add(new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = "string.Concat used inside loop. Consider using StringBuilder.",
                            Line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line,
                            Severity = Metadata.DefaultSeverity
                        });
                    }
                }

                // 🔥 Detect string interpolation inside loop
                var interpolations = loopBody.DescendantNodes()
                    .OfType<InterpolatedStringExpressionSyntax>();

                foreach (var interpolation in interpolations)
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "String interpolation inside loop may cause repeated allocations.",
                        Line = interpolation.GetLocation().GetLineSpan().StartLinePosition.Line,
                        Severity = Metadata.DefaultSeverity
                    });
                }
            }

            return issues;
        }

        private SyntaxNode GetLoopBody(SyntaxNode loop)
        {
            return loop switch
            {
                ForStatementSyntax fs => fs.Statement,
                ForEachStatementSyntax fe => fe.Statement,
                WhileStatementSyntax ws => ws.Statement,
                _ => null
            };
        }

        private HashSet<ISymbol> GetStringVariables(SyntaxNode loop, AnalysisContext context)
        {
            var result = new HashSet<ISymbol>();

            // Look outside loop scope (parent block)
            var parentBlock = loop.Parent;

            if (parentBlock == null)
                return result;

            var declarations = parentBlock.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>();

            foreach (var decl in declarations)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(decl);

                if (symbol is ILocalSymbol local &&
                    local.Type.SpecialType == SpecialType.System_String)
                {
                    result.Add(local);
                }
            }

            return result;
        }

        private bool IsInsideNestedLoop(SyntaxNode loop)
        {
            var parent = loop.Parent;

            while (parent != null)
            {
                if (parent is ForStatementSyntax ||
                    parent is ForEachStatementSyntax ||
                    parent is WhileStatementSyntax)
                {
                    return true;
                }

                parent = parent.Parent;
            }

            return false;
        }
    }
}