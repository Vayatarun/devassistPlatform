using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Rules.Design
{
    [RuleCategory("Design")]
    public class ShotgunSurgeryReporterRule : IRule
    {
        private const int ClassSpreadThreshold = 5;
        private const int CallCountThreshold = 10;

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "DESIGN004",
            Title = "Shotgun Surgery detected",
            Description = "Changes to this method may require modifications across many classes.",
            Category = "Design",
            DefaultSeverity = Severity.Major,
           // Remediation = "Reduce coupling by encapsulating behavior or redesigning responsibilities."
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            foreach (var kvp in context.GlobalStore.MethodUsageMap)
            {
                var method = kvp.Key;
                var usages = kvp.Value;

                var distinctClasses = usages
                    .Select(u => u.ClassName)
                    .Distinct()
                    .Count();

                var totalCalls = usages.Count;

                if (distinctClasses >= ClassSpreadThreshold ||
                    totalCalls >= CallCountThreshold)
                {
                    var severity = CalculateSeverity(distinctClasses, totalCalls);

                    foreach (var usage in usages)
                    {
                        issues.Add(new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = BuildMessage(method, distinctClasses, totalCalls),
                            FilePath = usage.FilePath,
                            Line = usage.Line,
                            Severity = severity
                        });
                    }
                }
            }

            return issues;
        }

        private Severity CalculateSeverity(int classes, int calls)
        {
            if (classes > 10 || calls > 25)
                return Severity.Critical;

            if (classes > 7)
                return Severity.Major;

            return Severity.Minor;
        }

        private string BuildMessage(string method, int classes, int calls)
        {
            return $"Method '{method}' is used across {classes} classes ({calls} calls). " +
                   "Changes may require widespread modifications (Shotgun Surgery).";
        }
    }
}