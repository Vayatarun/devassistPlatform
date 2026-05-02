# CodeAnalyzer Pro — User Manual

**Version 3.0** | Platform: .NET 8 | Language: C#

---

## Table of Contents

1. [What Is CodeAnalyzer Pro?](#1-what-is-codeanalyzer-pro)
2. [Prerequisites & Installation](#2-prerequisites--installation)
3. [Quick Start (60 seconds)](#3-quick-start-60-seconds)
4. [Running the Analyzer — All Modes](#4-running-the-analyzer--all-modes)
5. [Output Reports](#5-output-reports)
6. [Understanding Issues — Fix Guidance System](#6-understanding-issues--fix-guidance-system)
7. [All Rules Reference](#7-all-rules-reference)
8. [Quality Gate](#8-quality-gate)
9. [CI/CD Integration](#9-cicd-integration)
10. [REST API](#10-rest-api)
11. [Blazor Web UI](#11-blazor-web-ui)
12. [Adding a Custom Rule](#12-adding-a-custom-rule)
13. [Incremental Caching](#13-incremental-caching)
14. [Troubleshooting](#14-troubleshooting)

---

## 1. What Is CodeAnalyzer Pro?

CodeAnalyzer Pro is a **static code analysis platform** for C# projects. It scans your source code — or just the lines changed in a commit — and reports code quality issues across 8 categories:

| Category | What it checks |
|---|---|
| **Security** | SQL injection, hardcoded credentials, XSS, path traversal, weak crypto |
| **Stability** | Empty catch blocks, undisposed resources, null reference risks |
| **Efficiency** | LINQ in loops, multiple enumeration, blocking async calls |
| **Threading** | Task.Result/.Wait() deadlocks, async void, lock on public objects |
| **Design** | Deep nesting, magic numbers, God Classes, too many parameters |
| **Maintainability** | Cognitive complexity, duplicate code, shotgun surgery, feature envy |
| **Readability** | Naming conventions, long lines, complex conditions |
| **Documentation** | Missing XML docs, missing summary/param tags |

For every issue found it tells you:
- **What** is wrong and exactly **where** (file + line)
- **Why it matters** — the real-world risk or impact
- **Bad code example** — the problematic pattern
- **Good code example** — the correct fix
- **Suggested fix** — a specific, actionable instruction for this exact line

---

## 2. Prerequisites & Installation

### Requirements

| Requirement | Minimum version |
|---|---|
| .NET SDK | 8.0+ |
| Git | Any (for `--git` mode) |
| SVN | Any (for `--svn` mode) |
| Visual Studio | 2022+ (optional, for IDE integration) |

### Build from source

```bash
# Clone the repository
git clone <repo-url>
cd Analyzer1

# Build the full solution
dotnet build Analyzer1.sln

# Optional: build only the CLI tool
dotnet build CodeReviewReporter/CodeReviewReporter.csproj
```

> **Note:** The legacy `.vsix` project requires Visual Studio MSBuild and is expected to show 2 errors when built with `dotnet build`. All other projects build cleanly.

---

## 3. Quick Start (60 seconds)

### Analyze a folder of C# files

```bash
dotnet run --project CodeReviewReporter -- --path C:\MyProject\src --html --json
```

This creates three output files in your current directory:
- `CodeReviewIssues.xlsx` — Excel report (always generated)
- `CodeReviewReport.html` — Interactive HTML report
- `CodeReviewReport.json` — Machine-readable JSON

### Analyze only what changed in the last git commit

```bash
dotnet run --project CodeReviewReporter -- --git
```

---

## 4. Running the Analyzer — All Modes

### Command syntax

```
dotnet run --project CodeReviewReporter -- <mode> [options]
```

### Modes

#### `--path <folder>`
Recursively analyzes all `.cs` files in the given folder. Skips `bin\` and `obj\` directories. Files larger than 2 MB are skipped.

```bash
dotnet run --project CodeReviewReporter -- --path "C:\Projects\MyApp\src"
```

#### `--git`
Reads the diff between `HEAD~1` and `HEAD` (the last commit) and analyzes only the changed lines. Requires `git` on your PATH and must be run inside a git repository.

```bash
dotnet run --project CodeReviewReporter -- --git
```

#### `--svn`
Reads the current SVN working copy diff (`svn diff`) and analyzes only the changed lines.

```bash
dotnet run --project CodeReviewReporter -- --svn
```

#### `--file <diff-file>`
Analyzes a pre-saved diff file (unified diff format, as produced by `git diff` or `svn diff`).

```bash
# Save the diff first
git diff HEAD~1 HEAD > my_changes.diff

# Then analyze it
dotnet run --project CodeReviewReporter -- --file my_changes.diff
```

#### `--repo <git-url>`
Shallow-clones a git repository into a temp folder and analyzes the full codebase. Requires `git` on PATH.

```bash
dotnet run --project CodeReviewReporter -- --repo https://github.com/org/myrepo
```

### Output options (combine with any mode)

| Flag | Effect |
|---|---|
| _(none)_ | Always generates `CodeReviewIssues.xlsx` |
| `--html` | Also generates `CodeReviewReport.html` |
| `--json` | Also generates `CodeReviewReport.json` |

```bash
# Full output — all three reports
dotnet run --project CodeReviewReporter -- --path C:\MyProject --html --json
```

### Quality Gate options

| Flag | Default | Description |
|---|---|---|
| `--max-critical <n>` | `0` | Gate fails if critical issues exceed `n` |
| `--max-errors <n>` | `5` | Gate fails if error-level issues exceed `n` |
| `--fail-on-gate` | off | Returns exit code `1` when gate fails (for CI/CD) |

```bash
# Fail CI build if any critical issues are found
dotnet run --project CodeReviewReporter -- --path C:\src --fail-on-gate --max-critical 0
```

### Console output example

```
=== CodeAnalyzer Pro ===
Started: 14:23:01
Mode: Folder — C:\Projects\MyApp\src
Files found: 47

  Processed 10/47 files, 23 issues so far
  Processed 20/47 files, 51 issues so far
  Processed 47/47 files, 118 issues so far

─────────────────────────────────────────
  Analysis complete in 8.3s
  Total issues  : 118
  Critical      : 3
  Error         : 12
  Major         : 0
  Warning       : 45
  Info/Minor    : 58

  By category:
    Security              : 3
    Stability             : 14
    Efficiency            : 22
    Maintainability       : 31
    Readability           : 48
  Technical debt: 15h 44min (estimated)
─────────────────────────────────────────

  ✘  Quality Gate: FAILED
     · Critical issues: 3 (max allowed: 0)

Excel report generated: C:\...\CodeReviewIssues.xlsx
Done.
```

---

## 5. Output Reports

### 5.1 Excel Report (`CodeReviewIssues.xlsx`)

Always generated. Contains two worksheets:

#### "Summary" sheet
- Severity breakdown (Critical → Info) with counts
- Issues grouped by category
- Technical debt estimate (8 min per issue)

#### "Code Review" sheet — 12 columns

| Column | Content |
|---|---|
| **File** | Source file name |
| **Line** | Line number in the file |
| **Rule Id** | Rule identifier (e.g., `SEC001`) |
| **Category** | Security / Stability / Efficiency / etc. |
| **Severity** | Critical / Error / Warning / Info |
| **Message** | Specific description of the issue on this line |
| **Code (Actual)** | The actual line of code that triggered the rule |
| **Suggested Fix** | Concrete, actionable fix instruction |
| **Why It Matters** | Plain-English explanation of the risk |
| **Bad Code Example** | The pattern to avoid |
| **Good Code Fix** | The correct implementation |
| **Status** | `Open` (change to `Fixed` / `Accepted` / `Rejected`) |

Rows are color-coded by severity: red (Critical), salmon (Error), yellow (Warning).

### 5.2 HTML Report (`CodeReviewReport.html`)

An interactive single-file report viewable in any browser.

#### Features:
- **Metric cards** — Total, Critical, Error, Warning, Info counts at a glance
- **Quality Gate badge** — Green PASSED / Red FAILED with your configured thresholds
- **Category bar chart** — Visual breakdown of issues by category
- **Searchable table** — Filter by keyword or severity in real time
- **Export CSV** — Download the filtered view as a CSV file

#### Expandable issue detail panels (click any row):
Each issue row expands to show a full explanation panel:

```
▼  Row clicked — detail panel opens below
┌─────────────────────────────────────────────────────┐
│ WHY IT MATTERS                                       │
│ SQL Injection is OWASP Top 10 #1. An attacker can   │
│ bypass login, read all data, or drop your database. │
├──────────────────────────┬──────────────────────────┤
│ BAD CODE (Problem)       │ GOOD CODE (Fix)          │
│ ─────────────────────── │ ─────────────────────── │
│ string sql =             │ string sql =             │
│   "SELECT * FROM Users   │   "SELECT * FROM Users   │
│   WHERE Name='" +        │   WHERE Name=@name";     │
│   userName + "'";        │ cmd.Parameters           │
│ // ATTACK: ' OR 1=1--   │   .AddWithValue(          │
│                          │     "@name", userName);  │
├──────────────────────────┴──────────────────────────┤
│ SUGGESTED FIX                                        │
│ Replace string concatenation with:                  │
│ cmd.Parameters.AddWithValue("@param", value);        │
└─────────────────────────────────────────────────────┘
```

### 5.3 JSON Report (`CodeReviewReport.json`)

Machine-readable array of issues. Useful for integration with dashboards, custom tooling, or archiving.

```json
[
  {
    "ruleId": "SEC001",
    "title": "",
    "message": "Possible SQL Injection via string concatenation.",
    "category": "Security",
    "filePath": "C:\\src\\UserRepo.cs",
    "line": 47,
    "severity": "Critical",
    "suggestedFix": "Replace string concatenation with cmd.Parameters.AddWithValue...",
    "whyItMatters": "SQL Injection is OWASP Top 10 #1...",
    "badCodeExample": "...",
    "goodCodeExample": "..."
  }
]
```

---

## 6. Understanding Issues — Fix Guidance System

Every issue in the analyzer contains 4 layers of guidance:

### Layer 1 — Message
The specific problem found on this exact line. Examples:
- `Magic number '3000' detected. Replace with a named constant.`
- `Disposable object 'SqlConnection' is not wrapped in using`
- `Method 'ProcessOrder' has high cognitive complexity (28, threshold: 15)`

### Layer 2 — Suggested Fix
A concrete, line-specific instruction. Examples:
- `Replace '3000' with a named constant: private const int MEANINGFUL_NAME = 3000;`
- `Wrap in using: using var obj = new SqlConnection(...);`
- `Extract sub-logic from 'ProcessOrder' into private helper methods, and use guard clauses`

### Layer 3 — Why It Matters
A plain-English explanation of the real-world risk. Example for empty catch blocks:
> *"Empty catch blocks silently swallow exceptions, hiding bugs and making failures invisible. Your code appears to work while actually failing — making production debugging nearly impossible."*

### Layer 4 — Bad/Good Code Examples
Side-by-side before/after code showing the pattern to avoid and the correct fix. These are shown in the expandable HTML panel and in the Excel "Bad Code Example" / "Good Code Fix" columns.

---

## 7. All Rules Reference

### Severity key
| Severity | Meaning |
|---|---|
| **Critical** | Immediate security risk or will cause crashes/data loss |
| **Error** | Serious defect likely to cause bugs under real conditions |
| **Warning** | Code smell that degrades quality or reliability |
| **Info** | Improvement opportunity; low immediate risk |

---

### Security Rules

| Rule ID | Title | Severity | Description |
|---|---|---|---|
| **SEC001** | SQL Injection | Critical | SQL built with string concatenation or interpolation. An attacker can read or destroy your database. **Fix:** Use `cmd.Parameters.AddWithValue()` or an ORM. |
| **SEC002** | Hardcoded Credentials | Critical | Password, API key, or token stored in source code. **Fix:** Use `Environment.GetEnvironmentVariable()` or a secrets manager. |
| **SEC003** | XSS Vulnerability | Critical | User input rendered in HTML without encoding. **Fix:** Use `HttpUtility.HtmlEncode()` before output. |
| **SEC004** | Path Traversal | Critical | File path constructed from user input. **Fix:** Validate with `Path.GetFullPath()` against an allowed base path. |
| **SEC005** | Command Injection | Critical | Shell command constructed from user input. **Fix:** Use parameterized `ProcessStartInfo.ArgumentList`. |
| **SEC006** | Weak Cryptography | Error | Use of MD5 or SHA1 for security purposes. **Fix:** Use SHA256 or better. |
| **SEC007** | Insecure Random | Warning | `System.Random` used for security/token generation. **Fix:** Use `System.Security.Cryptography.RandomNumberGenerator`. |
| **SEC008** | Sensitive Data Logging | Warning | Passwords or tokens passed to a logging call. **Fix:** Never log raw credentials; log only an indicator. |
| **SEC009** | Open Redirect | Warning | `Response.Redirect()` called with user-controlled input. **Fix:** Validate redirect URL against an allowlist. |

---

### Stability Rules

| Rule ID | Title | Severity | Description |
|---|---|---|---|
| **R001** | Empty Catch Block | Warning | Exception silently swallowed — failures become invisible. **Fix:** Add `_logger.LogError(ex, ...); throw;` |
| **EX001** | Catch General Exception | Warning | `catch (Exception)` hides specific failures; unsafe recovery. **Fix:** Catch the specific exception type. |
| **STAB004** | Missing Dispose | Critical | IDisposable object not wrapped in `using` — resource leak. **Fix:** `using var obj = new Type(...);` |
| **STAB005** | Possible Null Reference | Warning | Dereference of a potentially null value. **Fix:** Use null-check or `?.` operator. |
| **STAB006** | Infinite Loop Risk | Warning | Loop with no detectable exit condition. **Fix:** Add a break condition or cancellation token. |
| **STAB007** | Index Out of Range | Warning | Array access without bounds check. **Fix:** Validate index against `.Length` before access. |

---

### Efficiency Rules

| Rule ID | Title | Severity | Description |
|---|---|---|---|
| **EFF001** | Multiple Enumeration | Critical | `IEnumerable` iterated more than once — query runs multiple times. **Fix:** `var list = source.ToList();` then use `list`. |
| **EFF003** | LINQ in Loop | Critical | LINQ inside a loop causes O(n²) complexity. **Fix:** Build a `Dictionary` before the loop, use O(1) lookup inside. |
| **EFF004** | Blocking Async Call | Critical | `.Result` or `.Wait()` on a Task — deadlock risk in ASP.NET. **Fix:** Make method `async Task<T>` and use `await`. |
| **EFF005** | String Concat in Loop | Warning | `+` string concatenation inside loop — O(n²) allocations. **Fix:** Use `StringBuilder` or `string.Join()`. |
| **EFF006** | Count Instead of Any | Info | `collection.Count() > 0` scans the full sequence. **Fix:** Use `collection.Any()` — stops at first element. |

---

### Threading Rules

| Rule ID | Title | Severity | Description |
|---|---|---|---|
| **THR003** | Task.Result / Wait | Critical | Blocking on async code — thread starvation and deadlocks. **Fix:** `await` the task; make the method `async`. |
| **THR004** | Async Void | Error | `async void` methods — exceptions are unobservable and crash the process. **Fix:** Use `async Task` instead. |
| **THR005** | Lock on Public Object | Warning | `lock(this)` or `lock(publicField)` — external code can deadlock you. **Fix:** Lock on a private `readonly object _lock = new();` |
| **THR006** | Thread.Sleep | Warning | `Thread.Sleep()` blocks the thread. **Fix:** Use `await Task.Delay()` in async context. |

---

### Design Rules

| Rule ID | Title | Severity | Description |
|---|---|---|---|
| **DES003** | Deep Nesting | Critical | Control structures nested >3 levels — hard to read and test. **Fix:** Invert conditions into guard clauses (early return). |
| **DES005** | Magic Number | Info | Unexplained numeric literal (not 0 or 1). **Fix:** `private const int DESCRIPTIVE_NAME = value;` |
| **DES006** | Too Many Parameters | Warning | Method has >5 parameters — hard to call correctly. **Fix:** Group related params into a parameter object. |
| **DES007** | Long Method | Warning | Method exceeds recommended line count. **Fix:** Extract sub-operations into private helper methods. |
| **DES008** | Large Class | Warning | Class exceeds recommended line/member count. **Fix:** Extract responsibilities into focused classes. |
| **DES009** | Inappropriate Intimacy | Warning | Class accesses internals of another class excessively. **Fix:** Move behavior to the class that owns the data. |
| **DESIGN002** | God Class | Info–Critical | Class does too many things (methods + fields + lines). **Fix:** Apply SRP — split into dedicated service classes. |
| **DES010** | Magic String | Info | Unexplained string literal used as key or constant. **Fix:** Use a `const string` or `nameof()`. |

---

### Maintainability Rules

| Rule ID | Title | Severity | Description |
|---|---|---|---|
| **MAIN003** | Cognitive Complexity | Info–Critical | Method is too complex to understand or safely modify. **Fix:** Extract sub-logic into small private methods; use guard clauses. |
| **MAIN004** | Duplicate Code | Warning | Identical or near-identical code blocks in multiple places. **Fix:** Extract to a shared method or base class. |
| **MAIN005** | Shotgun Surgery | Warning | A single change requires edits to many unrelated classes. **Fix:** Consolidate the scattered logic. |
| **MAIN006** | Feature Envy | Warning | Method uses another class's data more than its own. **Fix:** Move the method to the class it's most interested in. |
| **MAIN007** | Data Clump | Info | Same group of parameters repeatedly appear together. **Fix:** Group them into a class or struct. |
| **MAIN008** | Private Method Never Called | Info | Private method is defined but never called. **Fix:** Delete it (dead code increases maintenance burden). |

---

### Readability Rules

| Rule ID | Title | Severity | Description |
|---|---|---|---|
| **RD001** | Class Naming | Warning | Class name does not follow PascalCase. **Fix:** Rename to `PascalCase`. |
| **RD002** | Method Naming | Warning | Method name does not follow PascalCase. **Fix:** Rename to `PascalCase`. |
| **RD003** | Variable Naming | Info | Variable uses single-letter or unclear name. **Fix:** Use a descriptive name. |
| **RD004** | Field Naming | Info | Private field does not follow `_camelCase` convention. **Fix:** Rename to `_camelCase`. |
| **RD005** | Property Naming | Warning | Property does not follow PascalCase. **Fix:** Rename to `PascalCase`. |
| **RD006** | Constant Naming | Warning | Constant does not follow `UPPER_CASE` or `PascalCase`. **Fix:** Rename consistently. |
| **RD007** | Long Line | Info | Line exceeds 120 characters. **Fix:** Break into multiple lines. |
| **RD008** | Complex Condition | Warning | Boolean expression has >3 conditions. **Fix:** Extract to a named `bool` variable or method. |
| **RD009** | Poor Naming | Warning | Identifier name is too short or meaninglessly generic. **Fix:** Use a descriptive name. |

---

### Documentation Rules

| Rule ID | Title | Severity | Description |
|---|---|---|---|
| **DOC001** | Missing XML Doc | Info | Public type or member has no `///` documentation. **Fix:** Add `/// <summary>...</summary>`. |
| **DOC002** | Missing Summary Tag | Info | XML doc exists but `<summary>` is missing. **Fix:** Add a `<summary>` tag. |
| **DOC003** | Missing Param Doc | Info | Method has XML doc but `<param>` tags are missing. **Fix:** Document each parameter. |
| **DOC004** | Empty Documentation | Info | `<summary>` exists but is blank or meaningless. **Fix:** Write a real description. |

---

## 8. Quality Gate

The Quality Gate checks whether your codebase meets minimum standards before a release or merge.

### How it works

After analysis, the gate evaluates two thresholds:

| Threshold | CLI flag | Default |
|---|---|---|
| Max Critical issues | `--max-critical <n>` | 0 |
| Max Error issues | `--max-errors <n>` | 5 |

If either threshold is exceeded, the gate **FAILS** and the reasons are printed to the console.

### Triggering CI/CD failure

Use `--fail-on-gate` to return exit code `1` on gate failure (exit `0` on pass, `2` on fatal error):

```bash
dotnet run --project CodeReviewReporter -- --path ./src --fail-on-gate --max-critical 0 --max-errors 3
```

### Recommended thresholds

| Environment | `--max-critical` | `--max-errors` |
|---|---|---|
| Feature branch PR | 0 | 10 |
| Main branch merge | 0 | 3 |
| Production release | 0 | 0 |

---

## 9. CI/CD Integration

### GitHub Actions

```yaml
name: Code Quality Gate

on: [push, pull_request]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 2   # needed for --git mode

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore Analyzer1.sln

      - name: Build
        run: dotnet build Analyzer1.sln --no-restore

      - name: Run Code Analyzer
        run: |
          dotnet run --project CodeReviewReporter -- \
            --git \
            --html \
            --json \
            --fail-on-gate \
            --max-critical 0 \
            --max-errors 5

      - name: Upload Reports
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: code-analysis-report
          path: |
            CodeReviewIssues.xlsx
            CodeReviewReport.html
            CodeReviewReport.json
```

### Azure DevOps

```yaml
trigger:
  - main
  - develop

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '8.0.x'

  - script: dotnet restore Analyzer1.sln
    displayName: 'Restore'

  - script: dotnet build Analyzer1.sln --no-restore
    displayName: 'Build'

  - script: |
      dotnet run --project CodeReviewReporter -- \
        --path $(Build.SourcesDirectory)/src \
        --html --json \
        --fail-on-gate --max-critical 0
    displayName: 'Analyze Code Quality'

  - task: PublishBuildArtifacts@1
    condition: always()
    inputs:
      pathToPublish: '$(System.DefaultWorkingDirectory)'
      artifactName: 'CodeAnalysisReports'
      publishLocation: 'Container'
```

### Pre-commit hook (local)

Create `.git/hooks/pre-push` (make it executable with `chmod +x`):

```bash
#!/bin/bash
echo "Running CodeAnalyzer Pro on staged changes..."

dotnet run --project /path/to/Analyzer1/CodeReviewReporter -- \
  --git \
  --fail-on-gate \
  --max-critical 0 \
  --max-errors 10

if [ $? -ne 0 ]; then
    echo ""
    echo "ERROR: Quality Gate FAILED. Fix critical issues before pushing."
    exit 1
fi
```

---

## 10. REST API

The `Analyzer1.API` project exposes a Swagger-documented REST API.

### Start the API

```bash
dotnet run --project Analyzer1.API
```

Default URL: `https://localhost:7036`
Swagger UI: `https://localhost:7036/swagger`

### Key endpoints

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/projects` | List all projects |
| `POST` | `/api/projects` | Add a project |
| `GET` | `/api/issues` | List all issues |
| `GET` | `/api/issues?severity=Critical` | Filter by severity |
| `GET` | `/api/rules` | List all registered rules |
| `POST` | `/api/scan` | Trigger a scan |

> **Note:** The API currently uses an in-memory database. Restart clears all data. Engine wiring (connecting the API to the real analyzer engine) is in progress.

---

## 11. Blazor Web UI

The `Analyzer1.UI` project is a Blazor WebAssembly single-page application.

### Start the UI

```bash
# Start the API first
dotnet run --project Analyzer1.API

# Then start the UI
dotnet run --project Analyzer1.UI
```

The UI connects to `https://localhost:7036/`.

### Pages

| Page | Description |
|---|---|
| **Dashboard** | Metric cards + charts of issue counts by severity/category |
| **Issues** | Filterable/searchable table of all detected issues |
| **Projects** | Manage projects under analysis |
| **Rules** | Browse all rules, enable/disable, configure thresholds |
| **Code Review** | Inline code viewer with issues highlighted |

---

## 12. Adding a Custom Rule

Any class that implements `IRule` in a project whose name starts with `"Analyzer"` is **automatically discovered** at runtime — no registration needed.

### Step-by-step

#### 1. Create the rule file

Place it in `Analyzer1.Rules/<Category>/MyRule.cs`:

```csharp
using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer1.Rules.Design
{
    [RuleCategory("Design")]
    public class MyCustomRule : IRule
    {
        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId   = "DES099",                         // unique ID
            Title    = "My custom rule",
            Category = "Design",
            DefaultSeverity = Severity.Warning,
            Description     = "What this rule checks.",
            Remediation     = "How to fix it.",
            EffortMinutes   = 10,
            Tags            = new[] { "design", "custom" },

            // Fix guidance (shown in HTML + Excel reports)
            WhyItMatters    = "Why this matters to the team.",
            BadCodeExample  = "// BAD:\nvar x = DoTheBadThing();",
            GoodCodeExample = "// GOOD:\nvar x = DoItRight();"
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            if (context?.SyntaxRoot == null)
                yield break;

            // Example: flag every class named "Manager"
            foreach (var cls in context.SyntaxRoot
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.Text.EndsWith("Manager")))
            {
                yield return new CodeIssue
                {
                    RuleId   = Metadata.RuleId,
                    Message  = $"Class '{cls.Identifier.Text}' uses the vague 'Manager' suffix.",
                    Line     = cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    Severity = Metadata.DefaultSeverity,
                    SuggestedFix    = "Rename to reflect what it actually manages (e.g., UserRepository, OrderProcessor).",
                    WhyItMatters    = Metadata.WhyItMatters,
                    BadCodeExample  = Metadata.BadCodeExample,
                    GoodCodeExample = Metadata.GoodCodeExample
                };
            }
        }
    }
}
```

#### 2. Build and run — no other changes needed

```bash
dotnet build Analyzer1.Rules/Analyzer1.Rules.csproj
dotnet run --project CodeReviewReporter -- --path C:\MyProject
```

`RuleLoader` will find and run `DES099` automatically.

### Rules requiring cross-file data (Collector/Reporter pattern)

For rules that need to look across multiple files (e.g., finding duplicate code):

1. Create a **Collector** rule (`Phase = RuleExecutionPhase.Collector`) that writes data to `context.GlobalStore` during per-file analysis.
2. Create a **Reporter** rule (`Phase = RuleExecutionPhase.Reporter`) that reads `context.GlobalStore` and emits issues.

See `DuplicateCodeCollectorRule.cs` / `DuplicateCodeReporterRule.cs` for a working example.

---

## 13. Incremental Caching

When using `--path` or `--repo` mode, the analyzer caches a SHA-256 hash of each file's content in `analysis_cache.json` (created in the current directory).

On subsequent runs, **unchanged files are skipped** — only modified files are re-analyzed. This makes repeated runs on large codebases significantly faster.

### Cache behavior

| File changed? | Action |
|---|---|
| No (same hash) | Skipped — previous results used |
| Yes (new hash) | Re-analyzed, cache entry updated |
| New file | Analyzed and added to cache |

### Resetting the cache

Delete `analysis_cache.json` to force a full re-analysis:

```bash
del analysis_cache.json     # Windows
rm analysis_cache.json      # Linux/macOS
```

---

## 14. Troubleshooting

### No issues found

- Check that your target folder contains `.cs` files and is not empty.
- Ensure you are not pointing at a `bin\` or `obj\` directory — these are excluded.
- In `--git` mode, check that there are commits to diff (`git log --oneline` should show at least 2).

### Build error: "CodeTaskFactory is not supported"

This error comes from the legacy VSIX project (`Analyzer1.Vsix`) which requires Visual Studio's MSBuild. It is **unrelated to the CLI and analyzer functionality**. Build only the relevant projects:

```bash
dotnet build CodeReviewReporter/CodeReviewReporter.csproj
```

### Git/SVN diff returns no results

```bash
# Verify git diff has content
git diff HEAD~1 HEAD | head -20

# If empty, you may be on the first commit
# Use --path mode instead
dotnet run --project CodeReviewReporter -- --path .
```

### Excel report file is locked

If Excel is open with the previous report, the tool cannot overwrite it. Close the file in Excel, then re-run.

### Analysis is slow on large projects

- The first run processes all files; subsequent runs use the cache and are much faster.
- Very large files (>2 MB) are skipped automatically.
- Parallelism is set to `Environment.ProcessorCount` — no configuration needed.

### Rule not appearing in output

- Verify the class is `public` and in a namespace starting with `"Analyzer"`.
- Verify the class is in an assembly whose name starts with `"Analyzer"`.
- Verify the class implements `IRule` directly (not through an abstract base without the interface).

---

## Appendix — Rule ID Prefix Reference

| Prefix | Category |
|---|---|
| `SEC` | Security |
| `STAB` | Stability |
| `EFF` | Efficiency |
| `THR` | Threading |
| `DES` / `DESIGN` | Design |
| `MAIN` | Maintainability |
| `RD` | Readability |
| `DOC` | Documentation |
| `EX` / `R0` | Exception Handling |

---

*CodeAnalyzer Pro — Built with Roslyn, .NET 8, ClosedXML*
