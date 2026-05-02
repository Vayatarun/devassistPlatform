# CodeAnalyzer Pro — Product Documentation

> Enterprise-grade C# static analysis platform inspired by SonarQube.  
> Version 2.0 · .NET 8 · April 2026

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [Quick Start](#3-quick-start)
4. [CLI Reference](#4-cli-reference)
5. [Rules Catalog](#5-rules-catalog)
6. [Quality Gate](#6-quality-gate)
7. [Reports](#7-reports)
8. [Configuration](#8-configuration)
9. [Extending — Adding a New Rule](#9-extending--adding-a-new-rule)
10. [REST API](#10-rest-api)
11. [Blazor UI](#11-blazor-ui)
12. [CI/CD Integration](#12-cicd-integration)
13. [Comparison with SonarQube](#13-comparison-with-sonarqube)
14. [Roadmap](#14-roadmap)

---

## 1. Overview

**CodeAnalyzer Pro** is an open, extensible static analysis engine for C# (and JavaScript). It detects bugs, security vulnerabilities, code smells, maintainability issues, and style violations across your entire codebase or focused on a git/SVN diff.

### Key capabilities

| Capability | Details |
|---|---|
| **Rules** | 55+ built-in rules across 8 categories |
| **Security** | Taint-flow analysis: SQL Injection, XSS, Path Traversal, Command Injection, Hardcoded Credentials, Weak Crypto |
| **Dual pipeline** | Roslyn DiagnosticAnalyzer + custom IRule engine run in parallel, deduplicated |
| **Multi-language** | C# (Roslyn) · JavaScript/TypeScript (ESLint) · Razor `.cshtml` |
| **Reports** | Excel (rich, multi-sheet) · HTML (interactive dashboard) · JSON (CI/CD) |
| **Quality Gate** | Pass/Fail with configurable thresholds — exit code 1 on failure |
| **Incremental** | SHA-256 file-hash cache — unchanged files skipped automatically |
| **Parallel** | Analyses run in parallel bounded by CPU count |
| **Technical Debt** | Effort-minutes estimated per issue, displayed as hours/days |
| **API** | ASP.NET Core 8 REST API with Swagger |
| **UI** | Blazor WebAssembly dashboard |
| **VSIX** | Visual Studio extension — issues appear inline while coding |

---

## 2. Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                        Solution Layout                           │
│                                                                  │
│  Analyzer.Core/          ← Core contracts & models               │
│  ├── Interfaces/         IRule, IAnalyzerEngine, IReportGenerator│
│  ├── Models/             CodeIssue, RuleMetadata, AnalysisMetrics│
│  │                       QualityGate, AnalysisContext            │
│  ├── Services/           RuleExecutor, (QualityGateEvaluator)    │
│  ├── Registry/           RuleLoader, RuleRegistry                │
│  ├── Security/           TaintTrackingEngine                     │
│  └── Configuration/      RuleConfig, RuleConfigLoader            │
│                                                                  │
│  Analyzer1.Rules/        ← 55+ rules in 8 categories            │
│  ├── Security/           SqlInjection, XSS, PathTraversal, …     │
│  ├── Design/             DeepNesting, LargeClass, MagicNumber, …  │
│  ├── Maintainibility/    CognitiveComplexity, GodClass, DupCode   │
│  ├── Efficiency/         BlockingAsync, LinqInLoop, MultipleEnum  │
│  ├── Threading/          AsyncVoid, TaskResultWait, LockOnPublic  │
│  ├── Stability/          EmptyCatch, MissingDispose, NullRef      │
│  ├── Readability/        NamingRules, ComplexCondition, LongLine  │
│  └── Documentation/      MissingXmlDoc, MissingParamDoc          │
│                                                                  │
│  Analyzer.RoslynEngine/  ← Compiles & runs IRule engine          │
│  Analyzer.Roslyn/        ← VSIX DiagnosticAnalyzer bridge        │
│                                                                  │
│  CodeReviewReporter/     ← CLI entry point (primary)             │
│  ├── Services/           RoslynAnalyzerService (dual pipeline)   │
│  │                       ExcelReportService (multi-sheet)        │
│  │                       HtmlReportService (interactive HTML)    │
│  │                       JsonReportService (CI/CD)               │
│  │                       MultiLanguageAnalyzerService (JS+Razor) │
│  └── Program.cs          CLI with git/svn/path/repo/diff modes   │
│                                                                  │
│  Analyzer1.API/          ← ASP.NET Core REST API                 │
│  Analyzer1.UI/           ← Blazor WebAssembly dashboard          │
│  Analyzer1.Runner/       ← Dev test runner + HtmlReportGenerator │
│  Analyzer1/ (VSIX)       ← Visual Studio extension               │
└──────────────────────────────────────────────────────────────────┘
```

### Dual Analysis Pipeline

For every `.cs` file, two pipelines run concurrently:

```
File (.cs)
  │
  ├─▶ Pipeline 1: Roslyn DiagnosticAnalyzers
  │     Auto-loaded from assemblies starting with "Analyzer1"
  │     Includes VSIX-compatible analyzers
  │
  └─▶ Pipeline 2: Custom IRule Engine
        RuleLoader.LoadRules() discovers all IRule classes via reflection
        Rules use SyntaxRoot + SemanticModel + TaintEngine + GlobalStore

Both outputs ──▶ Deduplicate on (FileName, LineNumber, RuleId)
              ──▶ Unified List<CodeReviewIssue>
              ──▶ Excel + HTML + JSON reports
```

### Two-Phase Rules (Cross-File Analysis)

Duplicate code and shotgun surgery detection use a **Collector → Reporter** pattern:

```
Phase 1 (Collector): Runs per-file, populates GlobalAnalysisStore
Phase 2 (Reporter):  Reads store after all files analyzed, emits issues
```

---

## 3. Quick Start

### Prerequisites
- .NET 8 SDK
- Git (for `--git` mode)
- Node.js + ESLint (for JavaScript analysis)

### Build

```bash
dotnet build Analyzer1.sln
```

### Analyze a folder

```bash
dotnet run --project CodeReviewReporter -- --path C:\MyProject --html --json
```

This produces:
- `CodeReviewIssues.xlsx` — Excel report (always)
- `CodeReviewReport.html` — Interactive HTML dashboard
- `CodeReviewReport.json` — JSON for tooling/CI

### Analyze latest git commit

```bash
dotnet run --project CodeReviewReporter -- --git --html --fail-on-gate
```

---

## 4. CLI Reference

```
CodeAnalyzer Pro — Static Code Analyzer
========================================
Usage:
  --path <folder>       Analyze all .cs files in a folder
  --git                 Analyze the latest git diff (HEAD~1..HEAD)
  --svn                 Analyze the current SVN diff
  --file <diff-file>    Analyze a saved diff file
  --repo <git-url>      Clone and analyze a remote repository

Output options:
  --html                Generate HTML report  (CodeReviewReport.html)
  --json                Generate JSON report  (CodeReviewReport.json)
                        Excel is always generated (CodeReviewIssues.xlsx)

Quality Gate:
  --max-critical <n>    Fail gate if critical issues exceed n  (default 0)
  --max-errors   <n>    Fail gate if error issues exceed n     (default 5)
  --fail-on-gate        Return exit code 1 when gate fails (for CI/CD)

Examples:
  dotnet run --project CodeReviewReporter -- --path C:\MyProject --html --json
  dotnet run --project CodeReviewReporter -- --git --fail-on-gate --max-critical 0
  dotnet run --project CodeReviewReporter -- --repo https://github.com/org/repo --html
```

### Console output example

```
=== CodeAnalyzer Pro ===
Started: 14:22:05
Mode: Folder — C:\MyProject
Files found: 142
  Processed 10/142 files, 23 issues so far
  ...
  Processed 142/142 files, 187 issues so far

─────────────────────────────────────────
  Analysis complete in 8.3s
  Total issues  : 187
  Critical      : 3
  Error         : 12
  Major         : 8
  Warning       : 94
  Info/Minor    : 70

  By category:
    Security              : 3
    Maintainability       : 47
    Design                : 31
    Efficiency            : 22
    Threading             : 8
    Stability             : 14
    Readability           : 41
    Documentation         : 21

  Technical debt: 2d 4h (estimated)
─────────────────────────────────────────

  ✔  Quality Gate: PASSED
```

---

## 5. Rules Catalog

### Security (7 rules)

| Rule ID | Title | Severity | Effort |
|---|---|---|---|
| SEC001 | SQL Injection | Critical | 30 min |
| SEC002 | Hardcoded Credentials | Critical | 20 min |
| SEC003 | Command Injection | Critical | 30 min |
| SEC004 | Weak Cryptography | Critical | 25 min |
| SEC005 | Insecure Random | Warning | 15 min |
| SEC006 | Sensitive Data Logging | Warning | 20 min |
| SEC007 | Open Redirect | Error | 20 min |
| **SEC008** | **Cross-Site Scripting (XSS)** | Critical | 25 min |
| **SEC009** | **Path Traversal** | Critical | 30 min |

> Rules marked in **bold** were added in v2.0.

All security rules use the built-in **TaintTrackingEngine** to trace data flow from user-controlled sources (`Request`, `Query`, `Form`, `ReadLine`) through sanitizers to dangerous sinks (`Execute`, SQL strings, `Process.Start`, `InnerHTML`, file APIs).

### Design (6 rules)

| Rule ID | Title | Severity |
|---|---|---|
| DES001 | Large Class | Warning |
| DES002 | Long Method | Warning |
| DES003 | Deep Nesting | Critical |
| DES004 | Too Many Parameters | Warning |
| DES005 | Inappropriate Intimacy | Warning |
| DES006 | Magic Number | Warning |
| DES007 | Shotgun Surgery | Warning |

### Maintainability (6 rules)

| Rule ID | Title | Severity |
|---|---|---|
| MAIN001 | God Class | Warning |
| MAIN002 | Feature Envy | Warning |
| MAIN003 | High Cognitive Complexity | Info–Critical |
| MAIN004 | Duplicate Code | Warning |
| MAIN005 | Data Clump | Warning |
| MAIN010 | Shotgun Surgery | Warning |
| **MAIN011** | **Dead Private Method** | Warning |

### Efficiency (5 rules)

| Rule ID | Title | Severity |
|---|---|---|
| EFF001 | Blocking Async Call (.Result/.Wait()) | Warning |
| EFF002 | LINQ in Loop | Warning |
| EFF003 | Multiple Enumeration | Warning |
| EFF004 | String Concatenation in Loop | Warning |
| EFF005 | Count() Instead of Any() | Warning |

### Threading (4 rules)

| Rule ID | Title | Severity |
|---|---|---|
| THR001 | Async Void Method | Warning |
| THR002 | Task.Result / Task.Wait | Warning |
| THR003 | Lock on Public Object | Error |
| THR004 | Thread.Sleep in Async | Warning |

### Stability (6 rules)

| Rule ID | Title | Severity |
|---|---|---|
| STAB001 | Empty Catch Block | Warning |
| STAB002 | Catch General Exception | Warning |
| STAB003 | Missing Dispose | Warning |
| STAB004 | Async Void | Warning |
| STAB005 | Possible Null Reference | Warning |
| STAB006 | Infinite Loop | Error |
| STAB007 | Index Out of Range | Warning |

### Readability (10 rules)

| Rule ID | Title |
|---|---|
| RD001 | Class Naming Convention |
| RD002 | Method Naming Convention |
| RD003 | Property Naming Convention |
| RD004 | Field Naming Convention |
| RD005 | Constant Naming Convention |
| RD006 | Variable Naming Convention |
| RD007 | Poor Naming (single letters) |
| RD008 | Long Line (>120 chars) |
| RD009 | Complex Condition |
| RD010 | Magic String |

### Documentation (4 rules)

| Rule ID | Title |
|---|---|
| DOC001 | Missing XML Documentation |
| DOC002 | Missing `<summary>` tag |
| DOC003 | Missing `<param>` documentation |
| DOC004 | Empty documentation |

---

## 6. Quality Gate

The Quality Gate is a pass/fail checkpoint — like SonarQube's Quality Gate — that determines whether a build should succeed.

### Configuration (CLI flags)

```bash
--max-critical 0      # Zero tolerance for critical issues
--max-errors   5      # Up to 5 errors allowed
--fail-on-gate        # Return exit code 1 on failure (CI/CD)
```

### Gate Evaluation Logic

```
PASSED if:
  critical_count  ≤ max-critical   (default 0)
  AND
  error_count     ≤ max-errors     (default 5)

FAILED otherwise — reasons are listed in the console and HTML report
```

### Exit Codes

| Code | Meaning |
|---|---|
| 0 | Analysis completed successfully (gate passed or `--fail-on-gate` not set) |
| 1 | Quality Gate FAILED (only when `--fail-on-gate` is specified) |
| 2 | Fatal error (exception, missing path, etc.) |

---

## 7. Reports

### Excel Report (`CodeReviewIssues.xlsx`) — Always Generated

Two worksheets:

**Summary sheet** (auto-opens first):
- Severity breakdown (Critical / Error / Major / Warning / Minor / Info)
- Issues by category
- Technical debt estimate

**Code Review sheet** (filterable table):

| Column | Description |
|---|---|
| File | Source file path |
| Line | Line number |
| Rule Id | e.g. `SEC001` |
| Category | Security, Design, … |
| Severity | Color-coded rows |
| Message | Human-readable description |
| Code | The actual line of code |
| Suggested Fix | Recommended remediation |
| Reviewer Comment | Rule description |
| Status | "Open" (workflow field) |

### HTML Report (`--html`)

Self-contained, single-file HTML dashboard:

- **Quality Gate badge** — PASSED (green) / FAILED (red)
- **6 metric cards** — Total, Critical, Error, Warning, Info, Technical Debt
- **Issues by Category** — horizontal bar chart
- **Severity breakdown** — color-coded counts
- **Top 8 files** by issue count
- **Filterable issues table** — search by text, filter by severity/category
- **Export CSV** button — downloads visible rows

### JSON Report (`--json`)

Machine-readable output for CI/CD pipelines, dashboards, and integrations:

```json
{
  "generatedAt": "2026-04-29T10:22:05Z",
  "totalIssues": 187,
  "summary": {
    "critical": 3,
    "error": 12,
    "major": 8,
    "warning": 94,
    "minor": 20,
    "info": 50
  },
  "byCategory": {
    "Security": 3,
    "Maintainability": 47
  },
  "issues": [
    {
      "fileName": "UserService.cs",
      "lineNumber": 42,
      "ruleId": "SEC001",
      "category": "Security",
      "severity": "Critical",
      "message": "Possible SQL Injection via string concatenation.",
      "code": "var sql = \"SELECT * FROM Users WHERE Id = \" + userId;",
      "suggestedFix": "Use parameterized queries (SqlParameter) or Entity Framework."
    }
  ]
}
```

---

## 8. Configuration

### Per-Rule Configuration (`ruleconfig.json`)

Place a `ruleconfig.json` in the analyzed project root:

```json
{
  "rules": {
    "SEC001": { "enabled": true, "severity": "Critical" },
    "MAIN003": { "enabled": true, "parameters": { "maxComplexity": 20 } },
    "DOC001":  { "enabled": false },
    "DES003":  { "enabled": true, "severity": "Warning",
                 "parameters": { "maxDepth": 4 } }
  }
}
```

### Rule Settings Fields

| Field | Type | Description |
|---|---|---|
| `enabled` | bool | Enable or disable the rule |
| `severity` | string | Override default severity (`Info`, `Warning`, `Error`, `Critical`) |
| `parameters` | object | Numeric thresholds (rule-specific) |

### Incremental Cache (`analysis_cache.json`)

Automatically maintained. Maps `filePath → SHA256(content)`. Delete the file to force full re-analysis.

---

## 9. Extending — Adding a New Rule

1. **Create the class** in `Analyzer1.Rules/<Category>/`:

```csharp
using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer.Rules.Design
{
    [RuleCategory("Design")]
    public class TooManyPropertiesRule : IRule
    {
        private const int MaxProperties = 15;

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId      = "DES008",
            Title       = "Class has too many properties",
            Description = "A class with too many properties likely violates Single Responsibility.",
            Category    = "Design",
            DefaultSeverity = Severity.Warning,
            Remediation = "Split the class into smaller, focused classes.",
            EffortMinutes = 30,
            Tags        = new[] { "design", "srp", "class-size" }
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            if (context.SyntaxRoot == null) yield break;

            foreach (var cls in context.SyntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                int count = cls.DescendantNodes().OfType<PropertyDeclarationSyntax>().Count();
                if (count > MaxProperties)
                    yield return new CodeIssue
                    {
                        RuleId      = Metadata.RuleId,
                        Message     = $"Class '{cls.Identifier.Text}' has {count} properties (max {MaxProperties}).",
                        Line        = cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Severity    = Metadata.DefaultSeverity,
                        Category    = Metadata.Category,
                        Remediation = Metadata.Remediation,
                        EffortMinutes = Metadata.EffortMinutes
                    };
            }
        }
    }
}
```

2. **No registration required** — `RuleLoader` discovers it automatically via reflection.

3. **Run** `dotnet run --project CodeReviewReporter -- --path . --html` — the rule is live.

### Two-Phase Rule (Cross-File)

For rules that need data from multiple files (like duplicate detection):

```
Collector rule: Phase = RuleExecutionPhase.Collector
  → write data to context.GlobalStore

Reporter rule: Phase = RuleExecutionPhase.Analyzer
  → read from context.GlobalStore and emit CodeIssue
```

---

## 10. REST API

Base URL: `https://localhost:7036/api`

### Endpoints

#### Upload diff for analysis
```
POST /api/review/upload
Content-Type: multipart/form-data

Form fields:
  diffFile   — The diff file (.diff or .patch)
  Sheettype  — "0" = analysis report, "1" = enterprise checklist

Returns: Excel file (.xlsx)
```

#### Analyze a project (real engine in v2.0)
```
POST /api/analysis/scan
Body: { "projectId": 1, "path": "C:\\MyProject" }
Returns: { "issueCount": 42, "issueIds": [...] }
```

#### Get issues
```
GET /api/issues?projectId=1&severity=Critical
GET /api/issues/{id}
```

#### Get rules
```
GET /api/rules
GET /api/rules/{id}
```

#### Get projects
```
GET  /api/projects
POST /api/projects
```

Swagger UI available at: `https://localhost:7036/swagger`

---

## 11. Blazor UI

URL: `https://localhost:7036/` (served by the API project in development)

### Pages

| Page | Description |
|---|---|
| Dashboard | Summary charts: issues by severity, by category, recent scans |
| Issues | Filterable, sortable issue list with severity badges |
| Projects | Project management — register paths for scheduled scans |
| Rules | Full rule catalog with enable/disable toggles |
| Code Review | Upload a diff file and download the Excel report |

---

## 12. CI/CD Integration

### GitHub Actions

```yaml
- name: Run CodeAnalyzer Pro
  run: |
    dotnet run --project CodeReviewReporter -- \
      --path ${{ github.workspace }} \
      --git \
      --html \
      --json \
      --max-critical 0 \
      --max-errors 10 \
      --fail-on-gate

- name: Upload HTML report
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: code-analysis-report
    path: |
      CodeReviewReport.html
      CodeReviewReport.json
      CodeReviewIssues.xlsx
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run CodeAnalyzer Pro'
  inputs:
    command: 'run'
    projects: 'CodeReviewReporter/CodeReviewReporter.csproj'
    arguments: >
      -- --path $(Build.SourcesDirectory)
         --html --json
         --max-critical 0
         --fail-on-gate

- task: PublishBuildArtifacts@1
  inputs:
    PathtoPublish: 'CodeReviewReport.html'
    ArtifactName: 'CodeAnalysisReport'
```

### Reading the JSON report in CI

```bash
# Fail pipeline if any Critical issues
CRITICAL=$(cat CodeReviewReport.json | jq '.summary.critical')
if [ "$CRITICAL" -gt 0 ]; then
  echo "Build blocked: $CRITICAL critical security issues found"
  exit 1
fi
```

---

## 13. Comparison with SonarQube

| Feature | SonarQube Community | CodeAnalyzer Pro |
|---|---|---|
| Language support | 25+ | C#, JS, Razor (extensible) |
| Rules | 500+ | 55+ (growing) |
| Taint analysis | Yes (Enterprise) | Yes (built-in, Level 1) |
| Quality Gate | Yes | Yes |
| Technical Debt | Yes | Yes (effort-minutes) |
| Pull request analysis | Yes | Yes (git/svn diff) |
| HTML report | No (web UI only) | Yes (standalone file) |
| Excel report | No | Yes (multi-sheet) |
| JSON report | Yes | Yes |
| CI/CD exit codes | Yes | Yes |
| VS Code / VS inline | Via extension | Via VSIX |
| REST API | Yes | Yes |
| Web dashboard | Yes | Yes (Blazor) |
| Incremental analysis | Yes | Yes (SHA-256 cache) |
| Parallel analysis | Yes | Yes (CPU-bounded) |
| Cross-file analysis | Yes | Yes (GlobalStore) |
| Plugin SDK | Yes | Yes (IRule interface) |
| Open source | Partial | Full |
| Self-hosted | Yes | Yes |
| On-premise | Yes | Yes |
| Server required | Yes | No (CLI only) |
| Config file | `sonar-project.properties` | `ruleconfig.json` |

---

## 14. Roadmap

### v2.1 (Next)
- [ ] `ruleconfig.json` live-reload (watch mode)
- [ ] SARIF output format (GitHub Code Scanning)
- [ ] Suppressions via `[SuppressRule("SEC001")]` attribute
- [ ] Inter-procedural taint tracking (Level 2)
- [ ] NuGet package for `Analyzer.Core`

### v2.2
- [ ] TypeScript support (via ts-morph)
- [ ] VB.NET support
- [ ] Trend charts in HTML report (scan history)
- [ ] Webhook notifications (Slack, Teams)
- [ ] Rule severity ML suggestions based on fix history

### v3.0
- [ ] Distributed analysis server (multiple agents)
- [ ] Database persistence (PostgreSQL)
- [ ] OAuth2 / Azure AD authentication for API
- [ ] Pull Request decoration (GitHub, Azure DevOps)
- [ ] VS Code extension

---

## Appendix A — `CodeIssue` Model

```csharp
public record CodeIssue
{
    string RuleId        // e.g. "SEC001"
    string Title         // Short display title
    string Message       // Detailed, context-aware description
    string Category      // "Security", "Design", etc.
    string FilePath      // Absolute path to the source file
    int    Line          // 1-based line number
    int    Column        // 1-based column number
    Severity Severity    // Info | Warning | Error | Critical | Major | Minor
    DateTime DetectedAt  // UTC timestamp
    string Remediation   // How to fix it
    int    EffortMinutes // Estimated time to fix (technical debt)
    string[] Tags        // e.g. ["sql", "injection", "owasp-a03"]
}
```

## Appendix B — `RuleMetadata` Model

```csharp
public record RuleMetadata
{
    string RuleId           // Unique identifier
    string Title            // Short rule name
    string Description      // Detailed explanation
    string Category         // Rule category
    Severity DefaultSeverity
    RuleExecutionPhase Phase // Collector or Analyzer
    string Remediation      // Fix guidance
    int    EffortMinutes    // Technical debt per occurrence
    string[] Tags           // Searchable tags
    string DocumentationUrl // Link to full documentation
}
```

## Appendix C — Severity Levels

| Level | Numeric | Meaning | Typical Use |
|---|---|---|---|
| Info | 0 | Informational, no action required | Style hints |
| Warning | 1 | Should be fixed, not blocking | Code smells |
| Minor | 5 | Low impact | Minor naming issues |
| Major | 4 | Moderate impact | Performance issues |
| Error | 2 | Must be fixed | Logic bugs |
| Critical | 3 | Blocking, security or stability risk | SQL Injection, crashes |

---

*CodeAnalyzer Pro is developed and maintained as an internal enterprise tool.*  
*For issues, feature requests, or contributions — see the project repository.*
