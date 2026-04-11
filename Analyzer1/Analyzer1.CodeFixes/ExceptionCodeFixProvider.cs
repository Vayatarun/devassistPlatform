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

namespace Analyzer1
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExceptionCodeFixProvider)), Shared]
    public class ExceptionCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                ExceptionAnalyzer.EmptyCatchRuleId,
                ExceptionAnalyzer.ThrowWithoutMessageRuleId,
                ExceptionAnalyzer.MissingFinallyRuleId
            );

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            var span = diagnostic.Location.SourceSpan;

            if (diagnostic.Id == ExceptionAnalyzer.EmptyCatchRuleId)
            {
                var catchNode = root.FindToken(span.Start)
                    .Parent.AncestorsAndSelf()
                    .OfType<CatchClauseSyntax>()
                    .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Add logging inside catch block",
                        createChangedDocument: c => AddLogging(context.Document, catchNode, c),
                        equivalenceKey: "AddLogging"),
                    diagnostic);
            }

            if (diagnostic.Id == ExceptionAnalyzer.ThrowWithoutMessageRuleId)
            {
                var throwNode = root.FindToken(span.Start)
                    .Parent.AncestorsAndSelf()
                    .OfType<ThrowStatementSyntax>()
                    .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Add exception message",
                        createChangedDocument: c => AddExceptionMessage(context.Document, throwNode, c),
                        equivalenceKey: "AddExceptionMessage"),
                    diagnostic);
            }

            if (diagnostic.Id == ExceptionAnalyzer.MissingFinallyRuleId)
            {
                var tryNode = root.FindToken(span.Start)
                    .Parent.AncestorsAndSelf()
                    .OfType<TryStatementSyntax>()
                    .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Add finally block",
                        createChangedDocument: c => AddFinally(context.Document, tryNode, c),
                        equivalenceKey: "AddFinally"),
                    diagnostic);
            }
        }

        private async Task<Document> AddLogging(
            Document document,
            CatchClauseSyntax catchNode,
            CancellationToken cancellationToken)
        {
            var statement = SyntaxFactory.ParseStatement(
                "Console.WriteLine(ex.Message);");

            var newCatch = catchNode.WithBlock(
                SyntaxFactory.Block(statement));

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(catchNode, newCatch);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddExceptionMessage(
            Document document,
            ThrowStatementSyntax throwNode,
            CancellationToken cancellationToken)
        {
            var newThrow = SyntaxFactory.ParseStatement(
                "throw new Exception(\"Provide proper error message\");");

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(throwNode, newThrow);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddFinally(
            Document document,
            TryStatementSyntax tryNode,
            CancellationToken cancellationToken)
        {
            var finallyBlock = SyntaxFactory.FinallyClause(
                SyntaxFactory.Block(
                    SyntaxFactory.ParseStatement("// cleanup code")));

            var newTry = tryNode.WithFinally(finallyBlock);

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(tryNode, newTry);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}