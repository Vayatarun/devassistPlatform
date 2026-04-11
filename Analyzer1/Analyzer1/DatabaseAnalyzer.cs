using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Analyzer1
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DatabaseAnalyzer : DiagnosticAnalyzer
    {
        public const string UnnecessaryLoadRuleId = "CR201";
        public const string TransactionRuleId = "CR202";
        public const string EFDeleteRuleId = "CR203";
        public const string PresentationLayerRuleId = "CR204";

        private static DiagnosticDescriptor UnnecessaryLoadRule =
            new DiagnosticDescriptor(
                UnnecessaryLoadRuleId,
                "Avoid unnecessary database loading",
                "Data is being loaded unnecessarily from database",
                "Database",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static DiagnosticDescriptor TransactionRule =
            new DiagnosticDescriptor(
                TransactionRuleId,
                "Transaction required",
                "Multiple database operations detected without transaction",
                "Database",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static DiagnosticDescriptor EFDeleteRule =
            new DiagnosticDescriptor(
                EFDeleteRuleId,
                "Avoid loading entity before delete",
                "Delete entity without loading it first (Use Attach + Remove)",
                "Database",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static DiagnosticDescriptor PresentationLayerRule =
            new DiagnosticDescriptor(
                PresentationLayerRuleId,
                "Database call from Presentation Layer",
                "Avoid database calls directly from Presentation Layer",
                "Architecture",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                UnnecessaryLoadRule,
                TransactionRule,
                EFDeleteRule,
                PresentationLayerRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(CheckUnnecessaryLoad, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(CheckEFDeletePattern, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(CheckPresentationLayerAccess, SyntaxKind.InvocationExpression);
        }

      


        private static void CheckUnnecessaryLoad(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var symbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

            if (symbol == null)
                return;

            if (symbol.Name == "ToList" &&
                symbol.ContainingNamespace.ToDisplayString().StartsWith("System.Linq"))
            {
                var diagnostic = Diagnostic.Create(
                    UnnecessaryLoadRule,
                    invocation.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
        private static void CheckEFDeletePattern(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var methodName = invocation.Expression.ToString();

            if (methodName.Contains(".Remove("))
            {
                var diagnostic = Diagnostic.Create(
                    EFDeleteRule,
                    invocation.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void CheckPresentationLayerAccess(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var methodName = invocation.Expression.ToString();

            if (methodName.Contains("DbContext") || methodName.Contains("ExecuteQuery"))
            {
                var diagnostic = Diagnostic.Create(
                    PresentationLayerRule,
                    invocation.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}