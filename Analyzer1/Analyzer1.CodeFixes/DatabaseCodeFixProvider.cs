using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Analyzer1.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DatabaseCodeFixProvider)), Shared]
    public class DatabaseCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                DatabaseAnalyzer.UnnecessaryLoadRuleId,
                DatabaseAnalyzer.EFDeleteRuleId
            );

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var invocation = root.FindToken(diagnosticSpan.Start)
                                 .Parent
                                 .AncestorsAndSelf()
                                 .OfType<InvocationExpressionSyntax>()
                                 .FirstOrDefault();

            if (invocation == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Remove unnecessary ToList()",
                    createChangedDocument: c => RemoveToList(context.Document, invocation, c),
                    equivalenceKey: "RemoveToList"),
                diagnostic);
        }

        private async Task<Document> RemoveToList(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;

            if (memberAccess == null)
                return document;

            var newExpression = memberAccess.Expression;

            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newRoot = root.ReplaceNode(invocation, newExpression);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}