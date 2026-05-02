using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer1.Rules.Efficiency
{
    [RuleCategory("Efficiency")]
    public class MultipleEnumerationRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "EFF001",
            Title = "Avoid multiple enumeration of IEnumerable",
            Description = "Enumerating IEnumerable multiple times can cause performance issues.",
            Category = "Efficiency",
            DefaultSeverity = Severity.Critical,
            Remediation = "Call .ToList() or .ToArray() once to materialize the sequence, then use the list for all subsequent operations.",
            WhyItMatters = "Each enumeration of a lazy IEnumerable re-executes the query (hitting the DB, reading the file, etc.). Calling Count() then iterating fires the query twice. If the source changes between calls, results may differ silently.",
            BadCodeExample =
                "// BAD: GetUsers() query executes TWICE\n" +
                "IEnumerable<User> users = GetUsers();\n" +
                "int count = users.Count();           // Query #1 executed\n" +
                "var names = users.Select(u => u.Name); // Query #2 executed!",
            GoodCodeExample =
                "// GOOD: materialize once, reuse in-memory list\n" +
                "List<User> users = GetUsers().ToList(); // Query executes ONCE\n" +
                "int count = users.Count;             // In-memory, O(1)\n" +
                "var names = users.Select(u => u.Name); // Same data"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            var invocations = context.SyntaxRoot.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            var grouped = invocations
                .GroupBy(i => i.Expression.ToString());

            foreach (var group in grouped)
            {
                if (group.Count() > 1)
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = $"Possible multiple enumeration of IEnumerable: '{group.Key}' called {group.Count()} times. Materialize with .ToList() first.",
                        Line = group.First().GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Severity = Metadata.DefaultSeverity,
                        SuggestedFix = "Materialize the sequence once: var list = source.ToList(); — then use 'list' for all subsequent operations",
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
