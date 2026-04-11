using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer1
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string EmptyCatchRuleId = "EX001";
        public const string GenericCatchRuleId = "EX002";
        public const string ThrowWithoutMessageRuleId = "EX003";
        public const string MissingFinallyRuleId = "EX004";

        private static readonly DiagnosticDescriptor EmptyCatchRule =
            new DiagnosticDescriptor(
                EmptyCatchRuleId,
                "Empty catch block",
                "Catch block should not be empty",
                "ExceptionHandling",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor GenericCatchRule =
            new DiagnosticDescriptor(
                GenericCatchRuleId,
                "Avoid generic Exception catch",
                "Avoid catching generic Exception without proper handling",
                "ExceptionHandling",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ThrowWithoutMessageRule =
            new DiagnosticDescriptor(
                ThrowWithoutMessageRuleId,
                "Exception thrown without message",
                "Throw exception with meaningful message",
                "ExceptionHandling",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor MissingFinallyRule =
            new DiagnosticDescriptor(
                MissingFinallyRuleId,
                "Missing finally block",
                "Consider using finally block for resource cleanup",
                "ExceptionHandling",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                EmptyCatchRule,
                GenericCatchRule,
                ThrowWithoutMessageRule,
                MissingFinallyRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeCatchBlock, SyntaxKind.CatchClause);
            context.RegisterSyntaxNodeAction(AnalyzeThrowStatement, SyntaxKind.ThrowStatement);
            context.RegisterSyntaxNodeAction(AnalyzeTryStatement, SyntaxKind.TryStatement);
        }

        private static void AnalyzeCatchBlock(SyntaxNodeAnalysisContext context)
        {
            var catchClause = (CatchClauseSyntax)context.Node;

            if (catchClause.Block == null || !catchClause.Block.Statements.Any())
            {
                var diagnostic = Diagnostic.Create(
                    EmptyCatchRule,
                    catchClause.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }

            if (catchClause.Declaration != null &&
                catchClause.Declaration.Type.ToString() == "Exception")
            {
                var diagnostic = Diagnostic.Create(
                    GenericCatchRule,
                    catchClause.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
        {
            var throwStatement = (ThrowStatementSyntax)context.Node;

            if (throwStatement.Expression is ObjectCreationExpressionSyntax objectCreation)
            {
                if (objectCreation.ArgumentList == null ||
                    !objectCreation.ArgumentList.Arguments.Any())
                {
                    var diagnostic = Diagnostic.Create(
                        ThrowWithoutMessageRule,
                        throwStatement.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AnalyzeTryStatement(SyntaxNodeAnalysisContext context)
        {
            var tryStatement = (TryStatementSyntax)context.Node;

            if (tryStatement.Finally == null)
            {
                var diagnostic = Diagnostic.Create(
                    MissingFinallyRule,
                    tryStatement.TryKeyword.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}