using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Security
{
    [RuleCategory("Security")]
    public class HardcodedCredentialRule : IRule
    {
        private static readonly string[] Keywords =
            { "password", "pwd", "secret", "apikey", "token" };

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "SEC002",
            Title = "Hardcoded credentials detected",
            Description = "Sensitive credential stored directly in source code.",
            Category = "Security",
            DefaultSeverity = Severity.Critical,
            Remediation = "Store credentials in environment variables, Azure Key Vault, or a secrets manager. Never commit secrets to source control.",
            EffortMinutes = 20,
            Tags = new[] { "credentials", "secrets", "owasp-a02", "security" },
            WhyItMatters = "Secrets committed to source control are permanently visible in git history — even after deletion. Leaked credentials cause data breaches, unauthorized API charges, and compliance violations.",
            BadCodeExample =
                "// BAD: credentials hardcoded — exposed in git, logs, and build artifacts\n" +
                "string password = \"P@ssw0rd123!\";\n" +
                "string apiKey   = \"sk-abc123xyz987\";",
            GoodCodeExample =
                "// GOOD: read from environment or secrets manager at runtime\n" +
                "string password = Environment.GetEnvironmentVariable(\"DB_PASSWORD\");\n" +
                "// ASP.NET Core: string apiKey = _config[\"ApiKeys:ThirdParty\"];\n" +
                "// Production: use Azure Key Vault / AWS Secrets Manager"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var variables = context.SyntaxRoot.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>();

            foreach (var variable in variables)
            {
                var name = variable.Identifier.Text.ToLower();

                if (!Keywords.Any(k => name.Contains(k)))
                    continue;

                var initializer = variable.Initializer?.Value?.ToString();

                if (!string.IsNullOrEmpty(initializer) && initializer.Contains("\""))
                {
                    var varName = variable.Identifier.Text;
                    var envName = varName.ToUpperInvariant();
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Hardcoded credential in '{varName}'.",
                        Line = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Severity = Severity.Critical,
                        SuggestedFix = $"Move '{varName}' to an environment variable: Environment.GetEnvironmentVariable(\"{envName}\")",
                        WhyItMatters = Metadata.WhyItMatters,
                        BadCodeExample = Metadata.BadCodeExample,
                        GoodCodeExample = Metadata.GoodCodeExample
                    });
                }
            }

            return issues;
        }
    }
}