using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Maintainability
{
    [RuleCategory("Maintainibility")]
    public class DuplicateCodeReporterRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "MAIN002",
            Title = "Duplicate code detected across files",
            Description = "Duplicated code blocks found across multiple files or methods.",
            Category = "Maintainability",
            DefaultSeverity = Severity.Info,
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            foreach (var kvp in context.GlobalStore.DuplicateMap)
            {
                // Bug 13 fix: DuplicateMap values are now ConcurrentBag — materialize to List
                var occurrences = kvp.Value.ToList();

                if (occurrences.Count <= 1)
                    continue;

                var severity = occurrences.Count > 5 ? Severity.Critical : Metadata.DefaultSeverity;

                foreach (var occ in occurrences)
                {
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = BuildMessage(occurrences),
                        Line = occ.Line,       // already 1-based (fixed in Collector)
                        FilePath = occ.FilePath,
                        Severity = severity
                    });
                }
            }

            return issues;
        }

        private string BuildMessage(List<CodeBlockInfo> occurrences)
        {
            var locations = string.Join(", ",
                occurrences.Select(o => $"{o.MethodName} ({System.IO.Path.GetFileName(o.FilePath)}:{o.Line})"));

            return $"Duplicate code detected in: {locations}. Consider refactoring.";
        }
    }
}
