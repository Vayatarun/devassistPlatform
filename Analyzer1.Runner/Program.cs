using Analyzer.Core.Interfaces;
using Analyzer.Core.Registry;
using Analyzer.Core.Services;
using Analyzer.RoslynEngine;
using Analyzer1.Runner;


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

        //var reportGenerator = new HtmlReportGenerator();

        //await reportGenerator.GenerateAsync(
        //    issues,
        //    Path.Combine(Directory.GetCurrentDirectory(), "output"));

        Console.WriteLine("HTML report generated.");

        foreach (var issue in issues)
        {
            Console.WriteLine(
                $"{issue.FilePath} ({issue.Line}) - {issue.Message}");
        }
    }
    }
 