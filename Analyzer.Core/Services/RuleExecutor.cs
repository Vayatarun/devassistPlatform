using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using System;

namespace Analyzer.Core.Services;

public class RuleExecutor
{
    private readonly IRuleRegistry _registry;

    public RuleExecutor(IRuleRegistry registry)
    {
        _registry = registry;
    }

    public IEnumerable<CodeIssue> Execute(
        AnalysisContext context,
        IEnumerable<RuleConfiguration>? configurations = null)
    {
        var configDictionary = configurations?
            .ToDictionary(c => c.RuleId)
            ?? new Dictionary<string, RuleConfiguration>();

        foreach (var rule in _registry.GetAllRules())
        {
            if (configDictionary.TryGetValue(rule.Metadata.RuleId, out var config))
            {
                if (!config.IsEnabled)
                    continue;
            }

            IEnumerable<CodeIssue> issues;

            try
            {
                issues = rule.Analyze(context);
            }
            catch (Exception ex)
            {
                throw new Exceptions.RuleExecutionException(
                    rule.Metadata.RuleId, ex);
            }

            foreach (var issue in issues)
            {
                yield return ApplyConfiguration(issue, configDictionary);
            }
        }
    }

    private CodeIssue ApplyConfiguration(
        CodeIssue issue,
        Dictionary<string, RuleConfiguration> configs)
    {
        if (configs.TryGetValue(issue.RuleId, out var config) &&
            config.OverrideSeverity.HasValue)
        {
            return issue with { Severity = config.OverrideSeverity.Value };
        }

        return issue;
    }
}