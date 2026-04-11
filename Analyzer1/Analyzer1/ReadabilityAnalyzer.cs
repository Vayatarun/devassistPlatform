using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer1
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadabilityAnalyzer : DiagnosticAnalyzer
    {
        public const string MethodLengthRuleId = "CR001";
        public const string VariableNamingRuleId = "CR002";
        public const string CommentRuleId = "CR003";
        public const string ClassNamingRuleId = "CR004";

        private static readonly DiagnosticDescriptor MethodLengthRule =
            new DiagnosticDescriptor(
                MethodLengthRuleId,
                "Method too long",
                "Method '{0}' is too long ({1} lines). Consider splitting it.",
                "Code Readability",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor VariableNamingRule =
            new DiagnosticDescriptor(
                VariableNamingRuleId,
                "Variable naming convention",
                "Variable '{0}' should follow camelCase naming convention.",
                "Code Readability",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor CommentRule =
            new DiagnosticDescriptor(
                CommentRuleId,
                "Missing comment",
                "Public method '{0}' should have a comment describing its purpose.",
                "Code Readability",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ClassNamingRule =
            new DiagnosticDescriptor(
                ClassNamingRuleId,
                "Class naming convention",
                "Class '{0}' should follow PascalCase naming convention.",
                "Code Readability",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MethodLengthRule, VariableNamingRule, CommentRule, ClassNamingRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeVariable, SyntaxKind.VariableDeclarator);
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            var lineSpan = method.GetLocation().GetLineSpan();
            int lineCount = lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line;

            if (lineCount > 50)
            {
                var diagnostic = Diagnostic.Create(
                    MethodLengthRule,
                    method.Identifier.GetLocation(),
                    method.Identifier.Text,
                    lineCount);

                context.ReportDiagnostic(diagnostic);
            }

            if (method.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                var trivia = method.GetLeadingTrivia();

                bool hasComment = trivia.Any(x =>
                    x.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                    x.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                    x.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));

                if (!hasComment)
                {
                    var diagnostic = Diagnostic.Create(
                        CommentRule,
                        method.Identifier.GetLocation(),
                        method.Identifier.Text);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private void AnalyzeVariable(SyntaxNodeAnalysisContext context)
        {
            var variable = (VariableDeclaratorSyntax)context.Node;

            string variableName = variable.Identifier.Text;

            if (!string.IsNullOrEmpty(variableName) && char.IsUpper(variableName[0]))
            {
                var diagnostic = Diagnostic.Create(
                    VariableNamingRule,
                    variable.GetLocation(),
                    variableName);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classNode = (ClassDeclarationSyntax)context.Node;

            string className = classNode.Identifier.Text;

            if (!string.IsNullOrEmpty(className) && !char.IsUpper(className[0]))
            {
                var diagnostic = Diagnostic.Create(
                    ClassNamingRule,
                    classNode.Identifier.GetLocation(),
                    className);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}