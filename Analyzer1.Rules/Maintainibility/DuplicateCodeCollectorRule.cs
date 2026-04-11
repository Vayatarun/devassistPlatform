using Analyzer.Rules.Attributes;
using global::Analyzer.Core.Enums;
using global::Analyzer.Core.Interfaces;
using global::Analyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Security.Cryptography;
using System.Text;


   

    namespace Analyzer.Rules.Maintainability
{
    [RuleCategory("Maintainibility")]

    public class DuplicateCodeCollectorRule : IRule
        {
            private const int MinStatements = 5;

            public RuleMetadata Metadata => new RuleMetadata
            {
                RuleId = "MAIN002_COLLECT",
                Title = "Duplicate Code Collector",
                Category = "Maintainability",
                DefaultSeverity = Severity.Info
            };

            public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
            {
                var methods = context.SyntaxRoot.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Body != null);

                foreach (var method in methods)
                {
                    var statements = method.Body.Statements.ToList();

                    for (int i = 0; i <= statements.Count - MinStatements; i++)
                    {
                        var window = statements.Skip(i).Take(MinStatements).ToList();

                        var normalized = Normalize(window);
                        var hash = ComputeHash(normalized);

                        if (!context.GlobalStore.DuplicateMap.ContainsKey(hash))
                        {
                            context.GlobalStore.DuplicateMap[hash] = new List<CodeBlockInfo>();
                        }

                        context.GlobalStore.DuplicateMap[hash].Add(new CodeBlockInfo
                        {
                            FilePath = context.FilePath,
                            Line = window.First().GetLocation().GetLineSpan().StartLinePosition.Line,
                            MethodName = method.Identifier.Text
                        });
                    }
                }

                return Enumerable.Empty<CodeIssue>(); // Collector does not report
            }

            private string Normalize(IEnumerable<StatementSyntax> statements)
            {
                var sb = new StringBuilder();

                foreach (var stmt in statements)
                {
                    var normalized = stmt.ReplaceTokens(stmt.DescendantTokens(), (t, _) =>
                    {
                        if (t.IsKind(SyntaxKind.IdentifierToken))
                            return SyntaxFactory.Identifier("VAR");

                        if (t.IsKind(SyntaxKind.NumericLiteralToken))
                            return SyntaxFactory.Literal("NUM", 0);

                        if (t.IsKind(SyntaxKind.StringLiteralToken))
                            return SyntaxFactory.Literal("STR", "");

                        return t;
                    });

                    sb.Append(normalized.ToString());
                }

                return sb.ToString();
            }

            private string ComputeHash(string input)
            {
                using var sha = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha.ComputeHash(bytes);

                return string.Concat(hash.Select(b => b.ToString("x2")));
            }
        }
    }

