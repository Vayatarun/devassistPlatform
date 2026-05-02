# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build entire solution
dotnet build Analyzer1.sln

# Run CLI reporter (primary entry point)
dotnet run --project CodeReviewReporter -- --path <folder>
dotnet run --project CodeReviewReporter -- --git
dotnet run --project CodeReviewReporter -- --svn
dotnet run --project CodeReviewReporter -- --file <diff-file>
dotnet run --project CodeReviewReporter -- --repo <git-url>

# Run the REST API
dotnet run --project Analyzer1.API

# Run the Blazor UI
dotnet run --project Analyzer1.UI

# Run tests (Roslyn VSIX analyzer tests)
dotnet test Analyzer1\Analyzer1.Test\Analyzer1.Test.csproj

# Dev-testing runner (hardcoded path, for local use only)
dotnet run --project Analyzer1.Runner
```

## Architecture

This is a C# static analysis platform targeting .NET 8. The solution has two parallel rule-execution paths that converge in `CodeReviewReporter`.

### Core Layer — `Analyzer.Core/`

The framework that all other projects depend on:

- **`IRule`** — the central contract: `RuleMetadata Metadata { get; }` + `IEnumerable<CodeIssue> Analyze(AnalysisContext context)`. Every rule in `Analyzer1.Rules` implements this.
- **`AnalysisContext`** — passed to every rule; carries `SyntaxRoot`, `SemanticModel`, `TaintEngine`, `GlobalStore`, and `Config`.
- **`RuleLoader`** — reflection-based auto-discovery. Scans AppDomain assemblies for concrete `IRule` implementors whose `FullName` starts with `"Analyzer"`. No manual registration needed; adding a new rule class is enough.
- **`RuleRegistry` / `RuleExecutor`** — registry holds rules by `RuleId`; executor iterates them, handles enable/disable via `RuleConfiguration`, and allows per-rule severity override.
- **`TaintTrackingEngine`** — intra-method taint analysis (Level 1). Used by security rules to track data flow from sources (`Request`, `Form`, `ReadLine`) through sinks (`Execute`, `sql`, `Process.Start`).
- **`RuleConfig`** — per-rule settings: `Enabled`, optional `Severity` override, and a `Parameters` dictionary for numeric thresholds.

### Rules Layer — `Analyzer1.Rules/`

50+ rules implementing `IRule`, organized by folder/category:

| Category | Examples |
|---|---|
| Design | `DeepNestingRule`, `LargeClassRule`, `LongMethodRule`, `MagicNumberRule`, `InappropriateIntimacyRule` |
| Security | `SqlInjectionRule`, `CommandInjectionRule`, `HardcodedCredentialRule`, `SensitiveDataLoggingRule` |
| Maintainability | `CognitiveComplexityRule`, `GodClassRule`, `DuplicateCode*`, `FeatureEnvyRule` |
| Readability | naming rules (`ClassNamingRule`, `MethodNamingRule`, etc.), `ComplexConditionRule`, `LongLineRule` |
| Efficiency | `BlockingAsyncCallRule`, `MultipleEnumerationRule`, `LinqInLoopRule`, `StringConcatInLoopRule` |
| Threading | `AsyncVoidRule`, `TaskResultWaitRule`, `LockOnPublicObjectRule`, `ThreadSleepRule` |
| Stability | `EmptyCatchRule`, `CatchGeneralExceptionRule`, `MissingDisposeRule`, `InfiniteLoopRule` |
| Documentation | `MissingXmlDocumentationRule`, `MissingSummaryTagRule`, `MissingParamDocumentationRule` |

**Two-phase rules**: Duplicate code and shotgun surgery detection use a Collector → Reporter pattern. The Collector stores intermediate data in `GlobalAnalysisStore` during per-file analysis; the Reporter reads it afterward. Both rules must be loaded together.

Some rules (e.g., `DeepNestingRule`) also carry `[DiagnosticAnalyzer]` and work in the Roslyn pipeline.

### Roslyn Engine — `Analyzer.RoslynEngine/`

`RoslynAnalyzerEngine` implements `IAnalyzerEngine`. It compiles all `.cs` files in a directory into a `CSharpCompilation`, gets a `SemanticModel` per file, and runs the `RuleExecutor`. Used by `Analyzer1.Runner` for standalone project analysis.

### Roslyn Bridge — `Analyzer.Roslyn/`

`RuleWrapperAnalyzer` (a `DiagnosticAnalyzer`) auto-loads all `IRule` implementations and bridges them into the Roslyn analyzer pipeline so they work in Visual Studio and on `compilation.WithAnalyzers(...)`.

### CLI Reporter — `CodeReviewReporter/`

The production-ready CLI. `RoslynAnalyzerService.AnalyzeCode()` runs two pipelines per file in sequence:

1. **Roslyn DiagnosticAnalyzers** — auto-loaded from assemblies starting with `"Analyzer1"` via `compilation.WithAnalyzers(...)`.
2. **Custom `IRule` engine** — loaded via `RuleLoader.LoadRules()`, filtered by enabled categories.

Deduplication removes `(FileName, LineNumber, RuleId)` duplicates that might surface from both pipelines. Output is an Excel report (`CodeReviewIssues.xlsx`) via `ExcelReportService`. Incremental caching uses SHA-256 file hashes stored in `analysis_cache.json`.

### API + UI — `Analyzer1.API/` + `Analyzer1.UI/`

`Analyzer1.API` is an ASP.NET Core Web API (net8.0) with an in-memory EF Core database and Swagger. It exposes project/issue/rule management and scan triggering. The `AnalyzerService` currently returns hardcoded stub issues — real engine wiring is the next step.

`Analyzer1.UI` is Blazor WebAssembly (net7.0) pointing at `https://localhost:7036/`. Pages: Dashboard, Issues, Projects, Rules, CodeReview.

### Legacy VSIX — `Analyzer1/` subfolder

Standard Roslyn analyzer VSIX scaffold (`Analyzer1`, `Analyzer1.CodeFixes`, `Analyzer1.Package`, `Analyzer1.Vsix`, `Analyzer1.Test`). Tests use `Microsoft.CodeAnalysis.Testing` (MSTest).

## Adding a New Rule

1. Create a class in `Analyzer1.Rules/<Category>/` implementing `IRule`.
2. Implement `RuleMetadata` with a unique `RuleId` (e.g., `"SEC007"`), `Category`, `Title`, and `DefaultSeverity`.
3. Implement `Analyze(AnalysisContext context)` — use `context.SyntaxRoot` for syntax checks and `context.SemanticModel` for type-aware checks.
4. `RuleLoader` will pick it up automatically at runtime — no registration required.
5. If the rule needs cross-file data, use `context.GlobalStore` and follow the Collector/Reporter pattern.
