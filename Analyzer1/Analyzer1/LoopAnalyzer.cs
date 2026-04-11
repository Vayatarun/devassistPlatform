using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer1
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LoopAnalyzer : DiagnosticAnalyzer
    {
        public const string InfiniteLoopRuleId = "LOOP001";
        public const string LoopConditionRuleId = "LOOP002";
        public const string LoopIndexInitializationRuleId = "LOOP003";

        private static readonly DiagnosticDescriptor InfiniteLoopRule =
            new DiagnosticDescriptor(
                InfiniteLoopRuleId,
                "Potential infinite loop",
                "Loop condition may lead to infinite loop",
                "Loops",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor LoopConditionRule =
            new DiagnosticDescriptor(
                LoopConditionRuleId,
                "Loop termination condition not clear",
                "Loop termination condition should be clear and achievable",
                "Loops",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor LoopIndexInitializationRule =
            new DiagnosticDescriptor(
                LoopIndexInitializationRuleId,
                "Loop index not initialized",
                "Loop index should be properly initialized before loop",
                "Loops",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                InfiniteLoopRule,
                LoopConditionRule,
                LoopIndexInitializationRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeWhileLoop, SyntaxKind.WhileStatement);
            context.RegisterSyntaxNodeAction(AnalyzeForLoop, SyntaxKind.ForStatement);
        }

        private static void AnalyzeWhileLoop(SyntaxNodeAnalysisContext context)
        {
            var whileLoop = (WhileStatementSyntax)context.Node;

            // Detect while(true)
            if (whileLoop.Condition.ToString() == "true")
            {
                var diagnostic = Diagnostic.Create(
                    InfiniteLoopRule,
                    whileLoop.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeForLoop(SyntaxNodeAnalysisContext context)
        {
            var forLoop = (ForStatementSyntax)context.Node;

            // Check if loop condition exists
            if (forLoop.Condition == null)
            {
                var diagnostic = Diagnostic.Create(
                    LoopConditionRule,
                    forLoop.ForKeyword.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }

            // Check if initialization exists
            if (!forLoop.Declaration?.Variables.Any() ?? true)
            {
                var diagnostic = Diagnostic.Create(
                    LoopIndexInitializationRule,
                    forLoop.ForKeyword.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}