using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Core.Services;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
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

        var files = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

        var syntaxTrees = files
            .Select(file =>
                CSharpSyntaxTree.ParseText(
                    File.ReadAllText(file),
                    path: file))
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

        foreach (var tree in syntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            var root = await tree.GetRootAsync(cancellationToken);

            var context = new AnalysisContext
            {
                FilePath = tree.FilePath,
                SourceCode = root.ToString(),
                SyntaxRoot = root,
                SemanticModel = semanticModel
            };

            issues.AddRange(_executor.Execute(context));
        }

        return issues;
    }


}
