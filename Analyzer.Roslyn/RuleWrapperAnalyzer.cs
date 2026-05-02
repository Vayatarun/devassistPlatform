using Analyzer.Core.Interfaces;
using Analyzer.Core.Registry;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

// Aliases to avoid ambiguity with Microsoft.CodeAnalysis.Diagnostics.AnalysisContext
using CoreAnalysisContext = Analyzer.Core.Models.AnalysisContext;
using CoreGlobalStore = Analyzer.Core.Models.GlobalAnalysisStore;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RuleWrapperAnalyzer : DiagnosticAnalyzer
{
    private static readonly List<IRule> _rules = LoadRules();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        _rules.Select(r => new DiagnosticDescriptor(
            r.Metadata.RuleId,
            r.Metadata.Title,
            "{0}",
            "Custom",
            DiagnosticSeverity.Warning,
            true)).ToImmutableArray();

    // Bug 2 fix: Initialize takes Roslyn's AnalysisContext — no ambiguity now that
    // Analyzer.Core.Models is not wildcard-imported.
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(Analyze);
    }

    private void Analyze(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot();
        var coreContext = new CoreAnalysisContext
        {
            SyntaxRoot = root,
            SourceCode = root.ToString(),
            FilePath = context.Tree.FilePath,
            GlobalStore = new CoreGlobalStore()
        };

        foreach (var rule in _rules)
        {
            var issues = rule.Analyze(coreContext);

            foreach (var issue in issues)
            {
                var descriptor = SupportedDiagnostics
                    .FirstOrDefault(d => d.Id == rule.Metadata.RuleId);

                if (descriptor == null)
                    continue;

                var location = issue.Line > 0
                    ? Location.Create(context.Tree,
                        root.FindToken(root.GetText().Lines[issue.Line - 1].Start).Span)
                    : Location.None;

                context.ReportDiagnostic(Diagnostic.Create(descriptor, location, issue.Message));
            }
        }
    }

    private static List<IRule> LoadRules() => RuleLoader.LoadRules();
}
