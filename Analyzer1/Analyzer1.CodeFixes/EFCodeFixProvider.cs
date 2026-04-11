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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EFCodeFixProvider)), Shared]
    public class EFCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                EFAnalyzer.EFToListRuleId,
                EFAnalyzer.EFNoTrackingRuleId
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

            if (diagnostic.Id == EFAnalyzer.EFToListRuleId)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Remove unnecessary ToList()",
                        createChangedDocument: c => RemoveToList(context.Document, invocation, c),
                        equivalenceKey: "RemoveToList"),
                    diagnostic);
            }

            if (diagnostic.Id == EFAnalyzer.EFNoTrackingRuleId)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Add AsNoTracking()",
                        createChangedDocument: c => AddAsNoTracking(context.Document, invocation, c),
                        equivalenceKey: "AddAsNoTracking"),
                    diagnostic);
            }
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

        private async Task<Document> AddAsNoTracking(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;

            if (memberAccess == null)
                return document;

            var newExpression = SyntaxFactory.ParseExpression(
                memberAccess.Expression + ".AsNoTracking()");

            var newInvocation = invocation.WithExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    newExpression,
                    memberAccess.Name));

            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newRoot = root.ReplaceNode(invocation, newInvocation);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}