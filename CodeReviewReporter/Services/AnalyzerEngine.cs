using Analyzer.Core.Configuration;
using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeReviewReporter.Services
{
    public class AnalyzerEngine
    {
        private readonly List<IRule> _rules;
        private readonly RuleConfig _config;

        public AnalyzerEngine(IEnumerable<IRule> rules,RuleConfig ruleConfig)
        {
            _rules = rules.ToList();
            _config = ruleConfig;
        }

        public List<CodeIssue> Analyze(List<AnalysisContext> contexts)
        {
            var allIssues = new List<CodeIssue>();
            var globalStore = new GlobalAnalysisStore();

            // 🔹 STEP 1: Initialize contexts
            foreach (var context in contexts)
            {
                context.GlobalStore = globalStore;
                context.TaintEngine = new TaintTrackingEngine(context.SemanticModel);
            }

            // 🔹 STEP 2: Split rules by phase
            var collectors = _rules
                .Where(r => r.Metadata.Phase == RuleExecutionPhase.Collector)
                .ToList();

            var analyzers = _rules
                .Where(r => r.Metadata.Phase == RuleExecutionPhase.Analyzer)
                .ToList();

            // 🔹 PASS 1: Collector Rules
            foreach (var context in contexts)
            {
                foreach (var rule in collectors)
                {
                    rule.Analyze(context); // No issue expected
                }
            }

            // 🔹 PASS 2: Analyzer Rules
            foreach (var context in contexts)
            {
                foreach (var rule in analyzers)
                {
                    var issues = rule.Analyze(context);
                    if (issues != null)
                        allIssues.AddRange(issues);
                }
            }

            // 🔹 STEP 3: Deduplicate Issues
            return Deduplicate(allIssues);
        }

        private List<CodeIssue> Deduplicate(List<CodeIssue> issues)
        {
            return issues
                .GroupBy(i => new { i.RuleId, i.FilePath, i.Line })
                .Select(g => g.First())
                .ToList();
        }
    }
}
