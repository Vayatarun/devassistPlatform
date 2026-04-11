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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LoopCodeFixProvider)), Shared]
    public class LoopCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                LoopAnalyzer.InfiniteLoopRuleId,
                LoopAnalyzer.LoopConditionRuleId,
                LoopAnalyzer.LoopIndexInitializationRuleId
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

            if (diagnostic.Id == LoopAnalyzer.InfiniteLoopRuleId)
            {
                var whileNode = root.FindToken(span.Start)
                    .Parent.AncestorsAndSelf()
                    .OfType<WhileStatementSyntax>()
                    .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Replace infinite loop with condition",
                        createChangedDocument: c => ReplaceInfiniteLoop(context.Document, whileNode, c),
                        equivalenceKey: "ReplaceInfiniteLoop"),
                    diagnostic);
            }

            if (diagnostic.Id == LoopAnalyzer.LoopConditionRuleId)
            {
                var forNode = root.FindToken(span.Start)
                    .Parent.AncestorsAndSelf()
                    .OfType<ForStatementSyntax>()
                    .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Add loop termination condition",
                        createChangedDocument: c => AddLoopCondition(context.Document, forNode, c),
                        equivalenceKey: "AddLoopCondition"),
                    diagnostic);
            }

            if (diagnostic.Id == LoopAnalyzer.LoopIndexInitializationRuleId)
            {
                var forNode = root.FindToken(span.Start)
                    .Parent.AncestorsAndSelf()
                    .OfType<ForStatementSyntax>()
                    .First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Initialize loop index",
                        createChangedDocument: c => InitializeLoopIndex(context.Document, forNode, c),
                        equivalenceKey: "InitializeLoopIndex"),
                    diagnostic);
            }
        }

        private async Task<Document> ReplaceInfiniteLoop(
            Document document,
            WhileStatementSyntax whileNode,
            CancellationToken cancellationToken)
        {
            var newWhile = whileNode.WithCondition(
                SyntaxFactory.ParseExpression("condition"));

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(whileNode, newWhile);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddLoopCondition(
            Document document,
            ForStatementSyntax forNode,
            CancellationToken cancellationToken)
        {
            var newFor = forNode.WithCondition(
                SyntaxFactory.ParseExpression("i < limit"));

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(forNode, newFor);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> InitializeLoopIndex(
            Document document,
            ForStatementSyntax forNode,
            CancellationToken cancellationToken)
        {
            var declaration = SyntaxFactory.ParseStatement("int i = 0;");

            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newRoot = root.InsertNodesBefore(forNode, new[] { declaration });

            return document.WithSyntaxRoot(newRoot);
        }
    }
}