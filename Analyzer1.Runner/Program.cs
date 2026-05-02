using Analyzer.Core.Interfaces;
using Analyzer.Core.Registry;
using Analyzer.Core.Services;
using Analyzer.RoslynEngine;
using Analyzer1.Rules.Stability;  // Bug 6 fix: EmptyCatchRule is now in this namespace


class Program
{
    static async Task Main(string[] args)
    {
        var emptyCatchRule = new EmptyCatchRule();

        var rules = new List<IRule>
        {
            emptyCatchRule
        };

        var registry = new RuleRegistry(rules);
        Console.WriteLine("Rules Count: " + registry.GetAllRules().Count());

        var ruleExecutor = new RuleExecutor(registry);
        var engine = new RoslynAnalyzerEngine(ruleExecutor);

        var issues = await engine.AnalyzeProjectAsync(
            @"E:\Project\IIMU\Dev\EConnect.EEF.Web");

        Console.WriteLine("Analysis complete.");

        foreach (var issue in issues)
        {
            Console.WriteLine($"{issue.FilePath} ({issue.Line}) - {issue.Message}");
        }
    }
}
