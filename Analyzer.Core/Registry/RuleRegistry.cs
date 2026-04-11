using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;

namespace Analyzer.Core.Registry;

public class RuleRegistry : IRuleRegistry
{
    private readonly Dictionary<string, IRule> _rules;

    public RuleRegistry(IEnumerable<IRule> rules)
    {
        _rules = rules.ToDictionary(r => r.Metadata.RuleId);
    }

    public IEnumerable<IRule> GetAllRules()
        => _rules.Values;

    public IRule? GetRule(string ruleId)
        => _rules.TryGetValue(ruleId, out var rule)
            ? rule
            : null;

    public IEnumerable<IRule> GetEnabledRules(
    IEnumerable<RuleConfiguration> configs)
    {
        foreach (var config in configs)
        {
            if (config.IsEnabled &&
                _rules.TryGetValue(config.RuleId, out var rule))
            {
                yield return rule;
            }
        }
    }
}