using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer.Rules.Security
{
    [RuleCategory("Security")]
    public class XssRule : IRule
    {
        private static readonly string[] DangerousMethods =
            { "innerhtml", "document.write", "htmlraw", "raw(", "markupstring", "response.write" };

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "SEC008",
            Title = "Potential Cross-Site Scripting (XSS) vulnerability",
            Description = "User-controlled data may be rendered as unescaped HTML.",
            Category = "Security",
            DefaultSeverity = Severity.Critical,
            Remediation = "HTML-encode all user input before rendering. Use HtmlEncoder.Default.Encode() or Razor's default encoding. Avoid rendering raw HTML from untrusted sources.",
            EffortMinutes = 25,
            Tags = new[] { "xss", "owasp-a03", "injection", "security" }
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

                if (DangerousMethods.Any(m => text.Contains(m)))
                {
                    bool hasTaintedArg = context.TaintEngine != null &&
                                         context.TaintEngine.IsTaintedSink(invocation);

                    if (hasTaintedArg || ContainsUserInput(text))
                    {
                        yield return new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = "Potential XSS: unencoded user data may be written to HTML output.",
                            Line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                            Severity = Severity.Critical,
                            Category = Metadata.Category,
                            Remediation = Metadata.Remediation,
                            EffortMinutes = Metadata.EffortMinutes
                        };
                    }
                }
            }

            var assignments = context.SyntaxRoot.DescendantNodes()
                .OfType<AssignmentExpressionSyntax>();

            foreach (var assign in assignments)
            {
                var left = assign.Left.ToString().ToLower();
                var right = assign.Right.ToString().ToLower();

                if (left.Contains("innerhtml") &&
                    (right.Contains("request") || right.Contains("query") ||
                     right.Contains("form") || right.Contains("input")))
                {
                    yield return new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = "Potential XSS: user input assigned directly to innerHTML without encoding.",
                        Line = assign.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
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
            text.Contains("param");
    }
}
