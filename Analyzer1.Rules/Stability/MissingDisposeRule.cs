using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer1.Rules.Stability
{
    [RuleCategory("Stability")]
    public class MissingDisposeRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "STAB004",
            Title = "IDisposable object not disposed",
            DefaultSeverity = Severity.Critical,
            Description = "Objects implementing IDisposable should be disposed properly",
            Category = "Stability Issue",
            Remediation = "Wrap the object in a 'using' statement or declaration to guarantee disposal even when exceptions occur.",
            WhyItMatters = "IDisposable objects hold unmanaged resources (DB connections, file handles, sockets). Not disposing them causes connection pool exhaustion, file locks, and memory leaks that only manifest under load.",
            BadCodeExample =
                "// BAD: if an exception occurs, connection is never closed!\n" +
                "var conn = new SqlConnection(connStr);\n" +
                "conn.Open();\n" +
                "// ... work ...\n" +
                "conn.Close(); // skipped if exception thrown above",
            GoodCodeExample =
                "// GOOD: using guarantees Dispose() even on exception\n" +
                "using var conn = new SqlConnection(connStr);\n" +
                "conn.Open();\n" +
                "// ... work ...\n" +
                "// conn.Dispose() called automatically at end of scope"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var root = context.SyntaxRoot;

            // Bug 4 fix: SemanticModel can be null; fall back to syntax-only when not available
            var semanticModel = context.SemanticModel;

            var objectCreations = root.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>();

            foreach (var objectCreation in objectCreations)
            {
                bool isInsideUsing =
                    objectCreation.Ancestors().OfType<UsingStatementSyntax>().Any() ||
                    objectCreation.Ancestors().OfType<LocalDeclarationStatementSyntax>()
                        .Any(ld => ld.UsingKeyword != default);

                if (isInsideUsing)
                    continue;

                if (semanticModel != null)
                {
                    var typeInfo = semanticModel.GetTypeInfo(objectCreation).Type;
                    if (typeInfo == null) continue;

                    bool isDisposable = typeInfo.AllInterfaces
                        .Any(i => i.ToString() == "System.IDisposable");

                    if (!isDisposable) continue;

                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Disposable object '{typeInfo.Name}' is not wrapped in using",
                        Severity = Metadata.DefaultSeverity,
                        Line = objectCreation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        SuggestedFix = $"Wrap in using: using var obj = new {typeInfo.Name}(...);",
                        WhyItMatters = Metadata.WhyItMatters,
                        BadCodeExample = Metadata.BadCodeExample,
                        GoodCodeExample = Metadata.GoodCodeExample
                    };
                }
                else
                {
                    // Syntax-only: flag known IDisposable types by name
                    var typeName = objectCreation.Type.ToString();
                    var knownDisposable = new[] { "SqlConnection", "SqlCommand", "FileStream",
                        "StreamReader", "StreamWriter", "HttpClient", "MemoryStream",
                        "BinaryReader", "BinaryWriter", "CryptoStream" };

                    if (knownDisposable.Any(n => typeName.EndsWith(n)))
                    {
                        yield return new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = $"Disposable object '{typeName}' is not wrapped in using",
                            Severity = Metadata.DefaultSeverity,
                            Line = objectCreation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            SuggestedFix = $"Wrap in using: using var obj = new {typeName}(...);",
                            WhyItMatters = Metadata.WhyItMatters,
                            BadCodeExample = Metadata.BadCodeExample,
                            GoodCodeExample = Metadata.GoodCodeExample
                        };
                    }
                }
            }
        }
    }
}
