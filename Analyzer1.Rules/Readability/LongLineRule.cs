using Analyzer.Rules.Attributes;
using global::Analyzer.Core.Enums;
using global::Analyzer.Core.Interfaces;
using global::Analyzer.Core.Models;
using System.Collections.Generic;

    namespace Analyzer.Rules.Readability
    {
    [RuleCategory("Naming")]
    public class LongLineRule : IRule
        {
            private const int MaxLength = 120;

            public RuleMetadata Metadata => new RuleMetadata
            {
                RuleId = "READ001",
                Title = "Line too long",
                Category = "Readability",
                DefaultSeverity = Severity.Minor
            };

            public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
            {
                var issues = new List<CodeIssue>();
                var lines = context.SourceCode.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Length > MaxLength)
                    {
                        issues.Add(new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = $"Line exceeds {MaxLength} characters.",
                            Line = i,
                            Severity = Severity.Minor
                        });
                    }
                }

                return issues;
            }
        }
    }

