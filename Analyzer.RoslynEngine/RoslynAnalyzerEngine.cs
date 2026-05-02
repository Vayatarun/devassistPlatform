using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Core.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;

namespace Analyzer.RoslynEngine;

public class RoslynAnalyzerEngine : IAnalyzerEngine
{
    private readonly RuleExecutor _executor;

    public RoslynAnalyzerEngine(RuleExecutor executor)
    {
        _executor = executor;
    }

    public async Task<IReadOnlyList<CodeIssue>> AnalyzeProjectAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<CodeIssue>();

        var files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
            .ToList();

        var syntaxTrees = files
            .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file))
            .ToList();

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "AnalyzerCompilation",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Bug 3 fix: create one shared GlobalStore across all files so
        // Collector rules populate it before Reporter rules consume it.
        var globalStore = new GlobalAnalysisStore();

        foreach (var tree in syntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            var root = await tree.GetRootAsync(cancellationToken);

            var context = new AnalysisContext
            {
                FilePath = tree.FilePath,
                SourceCode = root.ToString(),
                SyntaxRoot = root,
                SemanticModel = semanticModel,
                GlobalStore = globalStore   // shared across all files
            };

            issues.AddRange(_executor.Execute(context));
        }

        return issues;
    }
}
