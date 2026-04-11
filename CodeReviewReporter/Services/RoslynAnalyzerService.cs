
   using Analyzer.Core;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Core.Registry;
using CodeReviewReporter.Models.CodeReviewReporter.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Collections.Immutable;
using AnalysisContext = Analyzer.Core.Models.AnalysisContext;

public class RoslynAnalyzerService
{
    // =========================
    // ✅ AUTO LOAD ROSLYN ANALYZERS
    // =========================
    public ImmutableArray<DiagnosticAnalyzer> GetAnalyzers()
    {
        var analyzers = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(t =>
                typeof(DiagnosticAnalyzer).IsAssignableFrom(t) &&
                !t.IsAbstract &&
                t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
            .ToList();

        return analyzers.ToImmutableArray();
    }

    // =========================
    // ✅ AUTO LOAD CODE FIX PROVIDERS
    // =========================
    public ImmutableArray<CodeFixProvider> GetCodeFixProviders()
    {
        var providers = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(t =>
                typeof(CodeFixProvider).IsAssignableFrom(t) &&
                !t.IsAbstract &&
                t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (CodeFixProvider)Activator.CreateInstance(t))
            .ToList();

        return providers.ToImmutableArray();
    }

    // =========================
    // ✅ MAIN ANALYSIS METHOD
    // =========================
    public async Task<List<CodeReviewIssue>> AnalyzeCode(string fileName, string code)
    {
        var issues = new List<CodeReviewIssue>();

        // Parse Syntax Tree
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var compilation = CSharpCompilation.Create("Analysis")
            .AddSyntaxTrees(tree)
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var codeLines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        // =========================
        // 🔹 1. ROSLYN ANALYZERS
        // =========================
        var analyzers = GetAnalyzers();
        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        foreach (var diagnostic in diagnostics)
        {
            // Skip low priority (optional)
            if (diagnostic.Severity == DiagnosticSeverity.Info)
                continue;

            var lineSpan = diagnostic.Location.GetLineSpan();
            int line = lineSpan.StartLinePosition.Line + 1;

            string codeLine = codeLines.ElementAtOrDefault(line - 1) ?? "";

            issues.Add(new CodeReviewIssue
            {
                FileName = fileName,
                LineNumber = line,
                RuleId = diagnostic.Id,
                Severity = diagnostic.Severity.ToString(),
                Message = diagnostic.GetMessage(),
                Description = diagnostic.Descriptor.Description?.ToString(),
                Code = codeLine,
                Category = diagnostic.Descriptor.Category
            });
        }

        // =========================
        // 🔹 2. CUSTOM RULE ENGINE (IRule)
        // =========================
        var context = new AnalysisContext
        {
            SyntaxRoot = root
            // Optional: add SemanticModel later if needed
        };

        var rules = RuleLoader.LoadRules();

        // OPTIONAL FILTERING (Enable/Disable categories)
        var enabledCategories = new List<string> { "Security", "Performance", "Design" };

        rules = rules
            .Where(r => enabledCategories.Contains(r.Metadata.Category))
            .ToList();

        foreach (var rule in rules)
        {
            try
            {
                var results = rule.Analyze(context);

                foreach (var r in results)
                {
                    issues.Add(new CodeReviewIssue
                    {
                        FileName = fileName,
                        LineNumber = r.Line + 1,
                        RuleId = r.RuleId,
                        Severity = r.Severity.ToString(),
                        Message = r.Message,
                        Description = rule.Metadata.Title,
                        Code = codeLines.ElementAtOrDefault(r.Line) ?? "",
                        Category = rule.Metadata.Category
                    });
                }
            }
            catch (Exception ex)
            {
                issues.Add(new CodeReviewIssue
                {
                    FileName = fileName,
                    RuleId = "RULE_ENGINE_ERROR",
                    Severity = "Critical",
                    Message = ex.Message,
                    Category = "Engine"
                });
            }
        }

        // =========================
        // 🔹 3. REMOVE DUPLICATES
        // =========================
        issues = issues
            .GroupBy(i => new { i.FileName, i.LineNumber, i.RuleId })
            .Select(g => g.First())
            .ToList();

        return issues;
    }
}
