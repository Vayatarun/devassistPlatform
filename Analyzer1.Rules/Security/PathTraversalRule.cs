using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer.Rules.Security
{
    [RuleCategory("Security")]
    public class PathTraversalRule : IRule
    {
        private static readonly string[] FileSinkMethods =
            { "file.read", "file.write", "file.open", "file.delete",
              "directory.get", "path.combine", "streamreader", "streamwriter" };

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "SEC009",
            Title = "Potential Path Traversal vulnerability",
            Description = "File system operation uses path derived from user input without sanitization.",
            Category = "Security",
            DefaultSeverity = Severity.Critical,
            Remediation = "Validate and sanitize file paths. Use Path.GetFullPath() and verify the result starts within an allowed base directory. Never pass raw user input to file system APIs.",
            EffortMinutes = 30,
            Tags = new[] { "path-traversal", "owasp-a01", "file-system", "security" }
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            if (context.SyntaxRoot == null)
                yield break;

            var invocations = context.SyntaxRoot.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var text = invocation.ToString().ToLower();

                if (!FileSinkMethods.Any(m => text.Contains(m)))
                    continue;

                bool hasTaintedArg = context.TaintEngine != null &&
                                     context.TaintEngine.IsTaintedSink(invocation);

                if (hasTaintedArg || ContainsUserInput(text))
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Potential path traversal: file system operation with user-controlled path.",
                        Line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Severity = Severity.Critical,
                        Category = Metadata.Category,
                        Remediation = Metadata.Remediation,
                        EffortMinutes = Metadata.EffortMinutes
                    };
                }
            }
        }

        private static bool ContainsUserInput(string text) =>
            text.Contains("request") || text.Contains("query") ||
            text.Contains("form") || text.Contains("input") ||
            text.Contains("param") || text.Contains("filename") ||
            text.Contains("filepath");
    }
}
