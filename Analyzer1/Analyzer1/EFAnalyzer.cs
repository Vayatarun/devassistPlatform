using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Analyzer1
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EFAnalyzer : DiagnosticAnalyzer
    {
        public const string EFDeleteRuleId = "EF001";
        public const string EFToListRuleId = "EF002";
        public const string EFSaveChangesLoopRuleId = "EF003";
        public const string EFNoTrackingRuleId = "EF004";

        private static readonly DiagnosticDescriptor EFDeleteRule =
            new DiagnosticDescriptor(
                EFDeleteRuleId,
                "Avoid loading entity before delete",
                "Entity is loaded before delete. Use Attach + Remove instead.",
                "EntityFramework",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor EFToListRule =
            new DiagnosticDescriptor(
                EFToListRuleId,
                "Avoid unnecessary ToList()",
                "Avoid calling ToList() unless required",
                "EntityFramework",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor EFSaveChangesLoopRule =
            new DiagnosticDescriptor(
                EFSaveChangesLoopRuleId,
                "SaveChanges inside loop",
                "Avoid calling SaveChanges() inside loops",
                "EntityFramework",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor EFNoTrackingRule =
            new DiagnosticDescriptor(
                EFNoTrackingRuleId,
                "Missing AsNoTracking",
                "Use AsNoTracking() for read-only queries",
                "EntityFramework",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                EFDeleteRule,
                EFToListRule,
                EFSaveChangesLoopRule,
                EFNoTrackingRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeLoops, SyntaxKind.ForEachStatement, SyntaxKind.ForStatement);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var methodName = invocation.Expression.ToString();

            if (methodName.Contains(".ToList("))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(EFToListRule, invocation.GetLocation()));
            }

            if (methodName.Contains(".Remove("))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(EFDeleteRule, invocation.GetLocation()));
            }

            if (methodName.Contains(".SaveChanges("))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(EFSaveChangesLoopRule, invocation.GetLocation()));
            }

            if (methodName.Contains(".Where("))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(EFNoTrackingRule, invocation.GetLocation()));
            }
        }

        private static void AnalyzeLoops(SyntaxNodeAnalysisContext context)
        {
            var loop = context.Node;

            var invocations = loop.DescendantNodes()
                                  .OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var methodName = invocation.Expression.ToString();

                if (methodName.Contains(".SaveChanges("))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(EFSaveChangesLoopRule, invocation.GetLocation()));
                }
            }
        }
    }
}