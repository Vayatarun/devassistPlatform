using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer1
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReadabilityCodeFixProvider)), Shared]
    public class ReadabilityCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(ReadabilityAnalyzer.VariableNamingRuleId);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var variable = root.FindToken(diagnosticSpan.Start)
                               .Parent
                               .AncestorsAndSelf()
                               .OfType<VariableDeclaratorSyntax>()
                               .First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Convert to camelCase",
                    createChangedDocument: c => ConvertToCamelCase(context.Document, variable, c),
                    equivalenceKey: "ConvertToCamelCase"),
                diagnostic);
        }

        private async Task<Document> ConvertToCamelCase(Document document,
            VariableDeclaratorSyntax variable,
            CancellationToken cancellationToken)
        {
            var variableName = variable.Identifier.Text;

            if (string.IsNullOrEmpty(variableName))
                return document;

            var newName = char.ToLower(variableName[0]) + variableName.Substring(1);

            var newIdentifier = SyntaxFactory.Identifier(newName)
                .WithTriviaFrom(variable.Identifier);

            var newVariable = variable.WithIdentifier(newIdentifier);

            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newRoot = root.ReplaceNode(variable, newVariable);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}