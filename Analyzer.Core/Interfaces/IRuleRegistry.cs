using Analyzer.Core.Models;

namespace Analyzer.Core.Interfaces;

public interface IRuleRegistry
{
    IEnumerable<IRule> GetAllRules();
    IRule? GetRule(string ruleId);

    IEnumerable<IRule> GetEnabledRules(
        IEnumerable<RuleConfiguration> configs);
}