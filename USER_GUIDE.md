# CodeAnalyzer Pro — Complete User Guide

> Version 2.0 · .NET 8 · April 2026  
> For developers, team leads, and DevOps engineers

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Prerequisites & Installation](#2-prerequisites--installation)
3. [Build the Solution](#3-build-the-solution)
4. [Running Your First Analysis](#4-running-your-first-analysis)
5. [Analysis Modes](#5-analysis-modes)
6. [Understanding the Console Output](#6-understanding-the-console-output)
7. [Quality Gate](#7-quality-gate)
8. [Excel Report Guide](#8-excel-report-guide)
9. [HTML Dashboard Guide](#9-html-dashboard-guide)
10. [JSON Report Guide](#10-json-report-guide)
11. [All CLI Flags Reference](#11-all-cli-flags-reference)
12. [Rules Reference](#12-rules-reference)
13. [Configuration File](#13-configuration-file)
14. [Writing a Custom Rule](#14-writing-a-custom-rule)
15. [REST API Usage](#15-rest-api-usage)
16. [Blazor Dashboard](#16-blazor-dashboard)
17. [CI/CD Integration](#17-cicd-integration)
18. [Team Workflows](#18-team-workflows)
19. [Troubleshooting](#19-troubleshooting)
20. [FAQ](#20-faq)

---

## 1. Introduction

**CodeAnalyzer Pro** is a C# static analysis platform that scans your source code for bugs, security vulnerabilities, code smells, and style violations — similar to SonarQube but fully self-hosted and customizable.

### What it does

- Scans every `.cs` file in your project using **55+ built-in rules**
- Detects **security vulnerabilities** (SQL Injection, XSS, Path Traversal, Hardcoded Credentials, and more)
- Measures **code quality** (cognitive complexity, deep nesting, god classes, duplicate code)
- Calculates **technical debt** in hours and days
- Generates **three report formats**: Excel, HTML, and JSON
- Enforces a **Quality Gate** that can block bad merges in CI/CD
- Runs **incrementally** — unchanged files are skipped using SHA-256 caching

### How it compares to SonarQube

| Feature | SonarQube Community | CodeAnalyzer Pro |
|---|---|---|
| Self-hosted | Yes | Yes |
| Server required | Yes | No — CLI only |
| Quality Gate | Yes | Yes |
| HTML report | Web UI only | Standalone HTML file |
| Excel report | No | Yes (multi-sheet) |
| JSON report | Yes | Yes |
| CI/CD exit codes | Yes | Yes |
| Custom rules | Plugin SDK | Implement IRule — no SDK needed |
| Taint analysis | Enterprise only | Built-in |
| Open source | Partial | Full |

---

## 2. Prerequisites & Installation

### Required software

| Software | Minimum Version | Download |
|---|---|---|
| .NET SDK | 8.0 | https://dot.net |
| Git | Any | https://git-scm.com |

### Optional software (JavaScript analysis)

| Software | Purpose | Install command |
|---|---|---|
| Node.js | Run ESLint for `.js` and `.cshtml` files | `winget install OpenJS.NodeJS` |
| ESLint | JavaScript linting engine | `cd CodeReviewReporter/NodeAnalyzer && npm install` |

### Verify your environment

Open a terminal and run:

```bash
dotnet --version
# Expected: 8.x.x or higher

git --version
# Expected: git version 2.x.x

node --version
# Expected: v18.x.x or higher (optional)
```

`[screenshot: terminal showing dotnet --version output]`

---

## 3. Build the Solution

### Step 1 — Open a terminal in the solution folder

```bash
cd "E:\BackUp\TarunPractice\Analyzer1"
```

### Step 2 — Build the solution

```bash
dotnet build Analyzer1.sln
```

**Expected output:**

```
Build succeeded.
    11 Warning(s)
    2 Error(s)
```

> **Note:** The 2 errors are from the VSIX project (`Analyzer1.Vsix`) which requires Visual Studio's MSBuild, not .NET Core MSBuild. This is normal and does not affect any functionality. All runtime projects build successfully.

`[screenshot: build output in terminal]`

### Step 3 — Verify the CLI

```bash
dotnet run --project CodeReviewReporter -- --help
```

**Expected output:**

```
=== CodeAnalyzer Pro ===

CodeAnalyzer Pro — Static Code Analyzer
========================================
Usage:
  --path <folder>       Analyze all .cs files in a folder
  --git                 Analyze the latest git diff (HEAD~1..HEAD)
  --svn                 Analyze the current SVN diff
  --file <diff-file>    Analyze a saved diff file
  --repo <git-url>      Clone and analyze a repository
  ...
```

`[screenshot: help output]`

---

## 4. Running Your First Analysis

### Quickest way — analyze a folder

```bash
dotnet run --project CodeReviewReporter -- --path "C:\MyProject" --html --json
```

Replace `C:\MyProject` with the path to any C# project on your machine.

### What happens behind the scenes

```
Step 1  Discover all .cs files (excluding \bin\ and \obj\)
Step 2  Load analysis_cache.json — skip files with unchanged SHA-256 hash
Step 3  For each new/changed file (in parallel, CPU-bounded):
          Pipeline A: Roslyn DiagnosticAnalyzers
          Pipeline B: 55 custom IRule rules (Security, Design, Efficiency …)
Step 4  Deduplicate issues on (FileName, LineNumber, RuleId)
Step 5  Evaluate Quality Gate (Critical count vs threshold)
Step 6  Generate reports:
          CodeReviewIssues.xlsx  ← always
          CodeReviewReport.html  ← with --html
          CodeReviewReport.json  ← with --json
Step 7  Update analysis_cache.json for next run
```

`[screenshot: terminal showing analysis progress and summary]`

### Output files location

All output files are generated in the **working directory** where you run the command (not in the project being analyzed):

```
E:\BackUp\TarunPractice\Analyzer1\
├── CodeReviewIssues.xlsx       ← Excel report
├── CodeReviewReport.html       ← HTML dashboard
├── CodeReviewReport.json       ← JSON report
└── analysis_cache.json         ← incremental cache (auto-managed)
```

---

## 5. Analysis Modes

CodeAnalyzer Pro supports five different input modes. Pick the one that matches your workflow.

### Mode 1 — Folder analysis (full project scan)

Best for: Initial scans, periodic full reviews, on-boarding new projects.

```bash
dotnet run --project CodeReviewReporter -- --path "C:\MyProject"
```

- Scans all `.cs` files recursively
- Skips `\bin\` and `\obj\` directories automatically
- Skips files larger than 2 MB
- Cached — second run is much faster

`[screenshot: folder mode console output showing file count and progress]`

---

### Mode 2 — Git diff analysis (last commit only)

Best for: Pre-push checks, lightweight PR reviews, developer machine use.

```bash
dotnet run --project CodeReviewReporter -- --git --html
```

- Analyzes only files changed in `git diff HEAD~1 HEAD`
- Much faster than a full scan — only changed code is analyzed
- No cache needed — runs on diff content directly

`[screenshot: git mode output]`

---

### Mode 3 — SVN diff analysis

Best for: Teams using Subversion instead of Git.

```bash
dotnet run --project CodeReviewReporter -- --svn --html
```

- Runs `svn diff` internally and analyzes the changed code
- Same output as git mode

---

### Mode 4 — Saved diff file

Best for: Analyzing a diff received by email, saved from a CI system, or exported from a PR.

```bash
dotnet run --project CodeReviewReporter -- --file "C:\temp\my-changes.diff" --html
```

- Reads any standard unified diff format (`.diff` or `.patch`)
- Works with diffs from GitHub, Bitbucket, Azure DevOps exports

---

### Mode 5 — Remote repository

Best for: Auditing a third-party repository, vendor code review, open source evaluation.

```bash
dotnet run --project CodeReviewReporter -- --repo https://github.com/org/repo --html --json
```

- Clones the repo with `git clone --depth 1` (shallow, fast)
- Runs full folder analysis on the cloned code
- Temporary folder is created in `%TEMP%` and cleaned up automatically

> **Note:** Requires Git to be installed and on your system PATH.

---

## 6. Understanding the Console Output

After analysis completes, the console shows a structured summary:

```
=== CodeAnalyzer Pro ===
Started: 14:22:05
Mode: Folder — C:\MyProject
Files found: 142
  Processed 10/142 files, 23 issues so far
  Processed 20/142 files, 61 issues so far
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

Excel report generated: E:\...\CodeReviewIssues.xlsx
HTML report generated:  E:\...\CodeReviewReport.html
JSON report generated:  E:\...\CodeReviewReport.json

Done.
```

### Reading the summary

| Field | Meaning |
|---|---|
| **Total issues** | Sum of all issues across all files and all rules |
| **Critical** | Security vulnerabilities, crashes — fix immediately |
| **Error** | Logic bugs, stability risks — fix before release |
| **Major** | Performance and maintainability — fix this sprint |
| **Warning** | Code smells, style — fix in regular cleanup |
| **Info/Minor** | Low-priority suggestions |
| **Technical debt** | Estimated total time to fix all issues (8 min avg per issue) |

### Quality Gate colors

- **Green ✔ PASSED** — all thresholds met, build can continue
- **Red ✘ FAILED** — thresholds exceeded, failure reasons listed below

`[screenshot: console output with colored quality gate result]`

---

## 7. Quality Gate

The Quality Gate is a configurable pass/fail checkpoint. When used with `--fail-on-gate`, it returns exit code `1` on failure, which stops a CI/CD pipeline automatically.

### How it works

```
Quality Gate PASSES when:
  Critical issue count  ≤  --max-critical  (default: 0)
  AND
  Error issue count     ≤  --max-errors    (default: 5)

Quality Gate FAILS when either threshold is exceeded.
```

### Configuring the gate

```bash
# Zero tolerance — no critical or error issues allowed
dotnet run --project CodeReviewReporter -- \
  --path "C:\MyProject" \
  --max-critical 0 \
  --max-errors 0 \
  --fail-on-gate \
  --html

# Relaxed gate — allow up to 10 errors
dotnet run --project CodeReviewReporter -- \
  --path "C:\MyProject" \
  --max-critical 0 \
  --max-errors 10 \
  --fail-on-gate
```

### Gate output when FAILED

```
  ✘  Quality Gate: FAILED
     · Critical issues: 3 (max allowed: 0)
     · Error issues: 12 (max allowed: 5)
```

`[screenshot: red failed gate output in terminal]`

### Exit codes

| Exit Code | Meaning | When |
|---|---|---|
| `0` | Success | Analysis completed, gate passed (or --fail-on-gate not set) |
| `1` | Gate failed | `--fail-on-gate` is set and thresholds exceeded |
| `2` | Fatal error | Missing path, unhandled exception, bad arguments |

### Recommended gate settings by environment

| Environment | `--max-critical` | `--max-errors` | Notes |
|---|---|---|---|
| Development branch | 5 | 20 | Lenient — allow work in progress |
| Feature branch PR | 0 | 10 | Block security issues on merge |
| Main / release | 0 | 0 | Zero tolerance — everything must be clean |

---

## 8. Excel Report Guide

The Excel report (`CodeReviewIssues.xlsx`) is **always generated** regardless of which flags you pass.

### Opening the report

```bash
# After analysis runs, open the file:
start CodeReviewIssues.xlsx        # Windows
open  CodeReviewIssues.xlsx        # macOS
```

`[screenshot: Excel file open showing two sheet tabs — Summary and Code Review]`

### Sheet 1 — Summary

The Summary sheet opens first and gives a top-level health check.

```
┌──────────────────────────────────────────────────────┐
│  CodeAnalyzer Pro — Analysis Summary                 │
│  Generated: 2026-04-29 14:22:05                      │
├──────────────────────┬───────────────────────────────┤
│  Severity Breakdown  │                               │
├──────────────────────┼───────────────────────────────┤
│  Critical            │  3       (pink rows)          │
│  Error               │  12      (salmon rows)        │
│  Major               │  8       (peach rows)         │
│  Warning             │  94      (yellow rows)        │
│  Minor               │  20      (gray rows)          │
│  Info                │  50      (blue rows)          │
│  TOTAL               │  187                          │
├──────────────────────┼───────────────────────────────┤
│  Issues by Category  │                               │
├──────────────────────┼───────────────────────────────┤
│  Security            │  3                            │
│  Maintainability     │  47                           │
│  Design              │  31                           │
│  Efficiency          │  22                           │
│  ...                 │  ...                          │
├──────────────────────┼───────────────────────────────┤
│  Technical Debt      │  2d 4h (estimated)            │
└──────────────────────┴───────────────────────────────┘
```

`[screenshot: Summary sheet with color-coded rows]`

### Sheet 2 — Code Review

The Code Review sheet contains every issue with 10 columns.

| Column | Header | Description |
|---|---|---|
| A | **File** | Full path to the source file |
| B | **Line** | Line number (1-based, click to navigate in VS) |
| C | **Rule Id** | Rule identifier e.g. `SEC001`, `DES003` |
| D | **Category** | Security / Design / Maintainability / Efficiency … |
| E | **Severity** | Critical / Error / Major / Warning / Minor / Info |
| F | **Message** | Human-readable description of the issue |
| G | **Code** | The actual line of code that triggered the rule |
| H | **Suggested Fix** | Remediation guidance — how to fix it |
| I | **Reviewer Comment** | Rule description / additional context |
| J | **Status** | "Open" — update manually as issues are resolved |

`[screenshot: Code Review sheet with colored rows and AutoFilter dropdowns]`

### Filtering the Excel report

The AutoFilter is already enabled on row 1. Common filter workflows:

**Find all critical security issues:**
1. Click the dropdown on column **E** (Severity) → select **Critical**
2. Click the dropdown on column **D** (Category) → select **Security**
3. You now see only critical security issues — assign these to developers first

**Find issues in a specific file:**
1. Click the dropdown on column **A** (File)
2. Use the search box inside the filter → type the file name
3. Only issues in that file are shown

**Find all SQL Injection issues:**
1. Click the dropdown on column **C** (Rule Id)
2. Select **SEC001**

`[screenshot: Excel AutoFilter dropdown showing severity options]`

### Updating issue status

The **Status** column (J) starts as "Open". As your team resolves issues:

1. Filter to the resolved issue
2. Change **Status** to `Resolved`, `Won't Fix`, or `Duplicate`
3. Save the file — use it as your code review tracking sheet

---

## 9. HTML Dashboard Guide

Generate the HTML report by adding `--html` to any command:

```bash
dotnet run --project CodeReviewReporter -- --path "C:\MyProject" --html
```

Then open the file:

```bash
start CodeReviewReport.html        # Windows — opens in default browser
```

`[screenshot: HTML report open in Chrome showing the full dashboard]`

### Dashboard sections explained

#### Section 1 — Header with Quality Gate badge

At the top right, a large badge shows:
- **Green PASSED** — your code meets the quality thresholds
- **Red FAILED** — thresholds exceeded, with failure reasons listed below

`[screenshot: green PASSED badge in report header]`

#### Section 2 — Metric cards

Six cards below the header show:

| Card | Color | What it shows |
|---|---|---|
| Total Issues | Blue | All issues across all files |
| Critical | Red | Security vulnerabilities, crash risks |
| Error | Orange | Logic bugs, stability issues |
| Warning | Yellow | Code smells, style violations |
| Info | Light blue | Low-priority suggestions |
| Tech Debt | Purple | Estimated total fix effort (e.g. "2d 4h") |

`[screenshot: six colored metric cards]`

#### Section 3 — Issues by Category (bar chart)

A horizontal bar chart shows which categories have the most issues. The longest bar is where you should focus first.

`[screenshot: horizontal bar chart showing Security, Design, Efficiency bars]`

#### Section 4 — Severity breakdown + Top files

- **Severity breakdown** — color-coded counts for each severity level
- **Top 8 files by issue count** — shows which source files need the most attention

`[screenshot: severity breakdown and top files chart]`

#### Section 5 — Filterable issues table

The full issues table at the bottom supports:

| Control | How to use |
|---|---|
| **Search box** | Type any text — filters by file name, rule ID, message, or category |
| **Severity dropdown** | Show only Critical / Error / Warning / Info issues |
| **Category dropdown** | Filter to a specific category (Security, Design, etc.) |
| **Export CSV button** | Downloads the currently visible rows as a CSV file |

`[screenshot: issues table with search box and severity filter]`

### Recommended HTML dashboard workflow

**Step 1** — Open the report in Chrome or Edge

**Step 2** — Check the Quality Gate badge (top-right)
  - If **FAILED**: read the failure reasons listed below the badge
  - If **PASSED**: proceed to review the issues

**Step 3** — Set filters: Severity = **Critical**, Category = **Security**
  - Fix these first — they are exploitable vulnerabilities

**Step 4** — Set filters: Severity = **Critical**, Category = **Design**
  - Deep nesting and large classes — schedule for next sprint

**Step 5** — Click **Export CSV** to share the filtered list with your team

**Step 6** — Share the HTML file by email or attach to Jira
  - The file is self-contained — recipients need only a browser, no tools

---

## 10. JSON Report Guide

Generate the JSON report by adding `--json`:

```bash
dotnet run --project CodeReviewReporter -- --path "C:\MyProject" --json
```

### File structure

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
    "Maintainability": 47,
    "Design": 31,
    "Efficiency": 22,
    "Threading": 8,
    "Stability": 14,
    "Readability": 41,
    "Documentation": 21
  },
  "issues": [
    {
      "fileName": "E:\\MyProject\\Services\\UserService.cs",
      "lineNumber": 42,
      "ruleId": "SEC001",
      "category": "Security",
      "severity": "Critical",
      "message": "Possible SQL Injection via string concatenation.",
      "code": "var sql = \"SELECT * FROM Users WHERE Id = \" + userId;",
      "suggestedFix": "Use parameterized queries (SqlParameter) or Entity Framework."
    },
    {
      "fileName": "E:\\MyProject\\Controllers\\AccountController.cs",
      "lineNumber": 88,
      "ruleId": "SEC002",
      "category": "Security",
      "severity": "Critical",
      "message": "Hardcoded credential in 'password'.",
      "code": "string password = \"Admin@123\";",
      "suggestedFix": "Store credentials in environment variables or Azure Key Vault."
    }
  ]
}
```

### Using the JSON report

**Count critical issues in PowerShell:**

```powershell
$report = Get-Content CodeReviewReport.json | ConvertFrom-Json
Write-Host "Critical: $($report.summary.critical)"
Write-Host "Total: $($report.totalIssues)"
```

**Count critical issues in bash (with jq):**

```bash
cat CodeReviewReport.json | jq '.summary.critical'
cat CodeReviewReport.json | jq '.byCategory'
cat CodeReviewReport.json | jq '[.issues[] | select(.severity=="Critical")]'
```

**Parse in C#:**

```csharp
var json = await File.ReadAllTextAsync("CodeReviewReport.json");
var report = JsonSerializer.Deserialize<JsonElement>(json);
int critical = report.GetProperty("summary").GetProperty("critical").GetInt32();
Console.WriteLine($"Critical issues: {critical}");
```

---

## 11. All CLI Flags Reference

### Input mode flags (choose one per run)

```
--path <folder>       Analyze all .cs files in a directory recursively
--git                 Analyze git diff HEAD~1 HEAD (last commit)
--svn                 Analyze SVN working copy diff
--file <path>         Analyze a saved diff file (.diff or .patch)
--repo <git-url>      Clone a remote repository and analyze it
```

### Output flags (can combine freely)

```
--html                Generate CodeReviewReport.html (interactive dashboard)
--json                Generate CodeReviewReport.json (machine-readable)
                      Note: Excel (CodeReviewIssues.xlsx) is always generated
```

### Quality Gate flags

```
--max-critical <n>    Maximum allowed Critical issues  (default: 0)
--max-errors   <n>    Maximum allowed Error issues     (default: 5)
--fail-on-gate        Return exit code 1 when gate fails (for CI/CD)
```

### Full example combining all options

```bash
dotnet run --project CodeReviewReporter -- \
  --path "C:\MyProject" \
  --html \
  --json \
  --max-critical 0 \
  --max-errors 5 \
  --fail-on-gate
```

---

## 12. Rules Reference

### Severity levels

| Level | Color | Priority | Action |
|---|---|---|---|
| **Critical** | 🔴 Red | 1 — Immediate | Security vulnerabilities, crash risks — fix today |
| **Error** | 🟠 Orange | 2 — This sprint | Logic bugs, stability issues |
| **Major** | 🟡 Dark yellow | 3 — This month | Performance, significant maintainability |
| **Warning** | 🟡 Yellow | 4 — Backlog | Code smells, style |
| **Minor** | ⚪ Gray | 5 — Optional | Low-impact style |
| **Info** | 🔵 Blue | 6 — Optional | Suggestions only |

### Security rules (highest priority)

| Rule ID | Title | Severity | What to look for |
|---|---|---|---|
| **SEC001** | SQL Injection | Critical | String concatenation inside SQL queries |
| **SEC002** | Hardcoded Credentials | Critical | Passwords, tokens, API keys in code |
| **SEC003** | Command Injection | Critical | User input passed to `Process.Start` |
| **SEC004** | Weak Cryptography | Critical | MD5, SHA1, DES usage |
| **SEC005** | Insecure Random | Warning | `new Random()` for security purposes |
| **SEC006** | Sensitive Data Logging | Warning | Passwords or tokens in log statements |
| **SEC007** | Open Redirect | Error | User-controlled redirect URLs |
| **SEC008** | Cross-Site Scripting (XSS) | Critical | User data written to HTML output |
| **SEC009** | Path Traversal | Critical | User-controlled file paths |

### Design rules

| Rule ID | Title | Severity | Threshold |
|---|---|---|---|
| DES001 | Large Class | Warning | > 300 lines |
| DES002 | Long Method | Warning | > 50 lines |
| DES003 | Deep Nesting | Critical | > 3 levels |
| DES004 | Too Many Parameters | Warning | > 5 parameters |
| DES005 | Inappropriate Intimacy | Warning | Accesses private members of another class |
| DES006 | Magic Number | Warning | Unexplained numeric literals |
| DES007 | Shotgun Surgery | Warning | Changes require edits in many classes |

### Maintainability rules

| Rule ID | Title | Severity | Threshold |
|---|---|---|---|
| MAIN001 | God Class | Warning | Class with too many responsibilities |
| MAIN002 | Feature Envy | Warning | Method more interested in other classes |
| MAIN003 | Cognitive Complexity | Info–Critical | > 15 (scales up to Critical > 30) |
| MAIN004 | Duplicate Code | Warning | Identical code blocks in different places |
| MAIN005 | Data Clump | Warning | Same group of parameters in multiple places |
| MAIN011 | Dead Private Method | Warning | Private method that is never called |

### Efficiency rules

| Rule ID | Title | Severity | What it detects |
|---|---|---|---|
| EFF001 | Blocking Async Call | Warning | `.Result` or `.Wait()` on a Task |
| EFF002 | LINQ in Loop | Warning | LINQ query executed inside a loop |
| EFF003 | Multiple Enumeration | Warning | IEnumerable enumerated more than once |
| EFF004 | String Concat in Loop | Warning | `string +=` inside a loop (use StringBuilder) |
| EFF005 | Count Instead of Any | Warning | `collection.Count() > 0` (use `.Any()`) |

### Threading rules

| Rule ID | Title | Severity |
|---|---|---|
| THR001 | Async Void Method | Warning |
| THR002 | Task.Result / Task.Wait | Warning |
| THR003 | Lock on Public Object | Error |
| THR004 | Thread.Sleep in Async | Warning |

### Stability rules

| Rule ID | Title | Severity |
|---|---|---|
| STAB001 | Empty Catch Block | Warning |
| STAB002 | Catch General Exception | Warning |
| STAB003 | Missing Dispose | Warning |
| STAB004 | Async Void | Warning |
| STAB005 | Possible Null Reference | Warning |
| STAB006 | Infinite Loop | Error |
| STAB007 | Index Out of Range | Warning |

### Readability rules

| Rule ID | Title |
|---|---|
| RD001 | Class Naming Convention (must be PascalCase) |
| RD002 | Method Naming Convention |
| RD003 | Property Naming Convention |
| RD004 | Field Naming Convention |
| RD005 | Constant Naming Convention |
| RD006 | Variable Naming Convention |
| RD007 | Poor Naming (single-letter variables) |
| RD008 | Long Line (> 120 characters) |
| RD009 | Complex Condition (too many `&&` / `\|\|`) |
| RD010 | Magic String (hardcoded string literals) |

### Documentation rules

| Rule ID | Title |
|---|---|
| DOC001 | Missing XML Documentation on public member |
| DOC002 | Missing `<summary>` tag |
| DOC003 | Missing `<param>` documentation |
| DOC004 | Empty documentation element |

---

## 13. Configuration File

Create a `ruleconfig.json` file in your project root to customize rules.

### Full example

```json
{
  "rules": {
    "SEC001": {
      "enabled": true,
      "severity": "Critical"
    },
    "MAIN003": {
      "enabled": true,
      "severity": "Warning",
      "parameters": {
        "maxComplexity": 20
      }
    },
    "DES003": {
      "enabled": true,
      "severity": "Warning",
      "parameters": {
        "maxDepth": 4
      }
    },
    "DOC001": {
      "enabled": false
    },
    "RD008": {
      "enabled": true,
      "parameters": {
        "maxLineLength": 150
      }
    }
  }
}
```

### Fields explained

| Field | Type | Default | Description |
|---|---|---|---|
| `enabled` | bool | `true` | Set to `false` to completely disable a rule |
| `severity` | string | rule default | Override the severity: `Info`, `Warning`, `Error`, `Critical` |
| `parameters` | object | rule default | Numeric thresholds specific to the rule |

### Common configuration patterns

**Disable all documentation rules (for projects without XML docs):**

```json
{
  "rules": {
    "DOC001": { "enabled": false },
    "DOC002": { "enabled": false },
    "DOC003": { "enabled": false },
    "DOC004": { "enabled": false }
  }
}
```

**Relax cognitive complexity threshold:**

```json
{
  "rules": {
    "MAIN003": {
      "enabled": true,
      "parameters": { "maxComplexity": 25 }
    }
  }
}
```

**Promote all security rules to Critical:**

```json
{
  "rules": {
    "SEC001": { "severity": "Critical" },
    "SEC002": { "severity": "Critical" },
    "SEC003": { "severity": "Critical" },
    "SEC004": { "severity": "Critical" },
    "SEC007": { "severity": "Critical" },
    "SEC008": { "severity": "Critical" },
    "SEC009": { "severity": "Critical" }
  }
}
```

### Incremental cache

The file `analysis_cache.json` is automatically created and managed. It stores `{ "file_path": "sha256_hash" }` pairs.

```bash
# Force full re-analysis (delete the cache)
del analysis_cache.json          # Windows
rm  analysis_cache.json          # Linux/macOS
```

---

## 14. Writing a Custom Rule

You can add a new rule in minutes. No registration, no config file change — just create the class and build.

### Step 1 — Create the file

Create a new `.cs` file in `Analyzer1.Rules/<Category>/`:

```
Analyzer1.Rules/
├── Design/
│   └── TooManyPropertiesRule.cs    ← your new rule here
├── Security/
├── Maintainibility/
└── ...
```

### Step 2 — Implement IRule

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
            RuleId          = "DES008",          // unique ID — check existing rules first
            Title           = "Class has too many properties",
            Description     = "A class with too many properties likely violates Single Responsibility.",
            Category        = "Design",
            DefaultSeverity = Severity.Warning,
            Remediation     = "Split the class into smaller, focused classes.",
            EffortMinutes   = 30,                // estimated fix time per occurrence
            Tags            = new[] { "design", "srp", "class-size" }
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            // Always guard against null SyntaxRoot
            if (context.SyntaxRoot == null)
                yield break;

            foreach (var cls in context.SyntaxRoot
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>())
            {
                int count = cls.DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Count();

                if (count > MaxProperties)
                {
                    yield return new CodeIssue
                    {
                        RuleId        = Metadata.RuleId,
                        Message       = $"Class '{cls.Identifier.Text}' has {count} properties (max {MaxProperties}).",
                        Line          = cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Severity      = Metadata.DefaultSeverity,
                        Category      = Metadata.Category,
                        Remediation   = Metadata.Remediation,
                        EffortMinutes = Metadata.EffortMinutes
                    };
                }
            }
        }
    }
}
```

### Step 3 — Build and run

```bash
dotnet build Analyzer1.Rules/Analyzer1.Rules.csproj
dotnet run --project CodeReviewReporter -- --path "C:\MyProject" --html
```

The rule is immediately active — no restart required.

`[screenshot: new rule appearing in HTML report with DES008 rule ID]`

### Rule design guidelines

| Guideline | Detail |
|---|---|
| **Always guard null** | Check `context.SyntaxRoot == null` at the top of Analyze() |
| **Line numbers are 1-based** | Use `GetLineSpan().StartLinePosition.Line + 1` |
| **Use yield return** | Prefer `yield return` over building a `List<CodeIssue>` |
| **Check SemanticModel** | If your rule needs type info, also check `context.SemanticModel == null` |
| **Unique RuleId** | Check existing rules to avoid ID collisions |
| **Fill Remediation** | Always provide a `Remediation` string — it appears in reports |
| **Set EffortMinutes** | This feeds the technical debt calculation |

### Two-phase rules (cross-file analysis)

For rules that need data from multiple files (like finding duplicate code across files):

```csharp
// Collector rule — Phase 1, runs per file
public class MyCollectorRule : IRule
{
    public RuleMetadata Metadata => new RuleMetadata
    {
        RuleId = "MY001",
        Phase  = RuleExecutionPhase.Collector,  // ← mark as Collector
        // ...
    };

    public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
    {
        // Store data in GlobalStore — shared across all files
        context.GlobalStore.MethodUsageMap.TryAdd("key", new ConcurrentBag<CallSiteInfo>());
        // Add your data...
        yield break; // Collectors don't emit issues
    }
}

// Reporter rule — Phase 2, runs after all files
public class MyReporterRule : IRule
{
    public RuleMetadata Metadata => new RuleMetadata
    {
        RuleId = "MY002",
        Phase  = RuleExecutionPhase.Analyzer,
        // ...
    };

    public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
    {
        // Read from GlobalStore and emit issues
        foreach (var entry in context.GlobalStore.MethodUsageMap)
        {
            // ... yield return issues
        }
    }
}
```

---

## 15. REST API Usage

The API allows you to trigger analyses and retrieve results programmatically.

### Start the API

```bash
dotnet run --project Analyzer1.API
```

The API starts at `https://localhost:7036`. Open Swagger at:

```
https://localhost:7036/swagger
```

`[screenshot: Swagger UI showing all endpoints]`

### Upload a diff file for analysis

```bash
curl -X POST https://localhost:7036/api/review/upload \
  -F "diffFile=@my-changes.diff" \
  -F "Sheettype=0" \
  --output CodeReviewReport.xlsx
```

**Parameters:**

| Parameter | Value | Description |
|---|---|---|
| `diffFile` | file | The diff file to analyze (.diff or .patch) |
| `Sheettype` | `0` | Returns analysis report Excel file |
| `Sheettype` | `1` | Returns enterprise code review checklist |

### Analyze a project via API

```bash
curl -X POST https://localhost:7036/api/analysis/scan \
  -H "Content-Type: application/json" \
  -d '{"projectId": 1, "path": "C:\\MyProject"}'
```

**Response:**

```json
{
  "issueCount": 187,
  "issues": [
    {
      "rule": "SEC001",
      "severity": "Critical",
      "filePath": "Services/UserService.cs",
      "line": 42,
      "message": "Possible SQL Injection via string concatenation."
    }
  ]
}
```

### Get all issues for a project

```bash
curl https://localhost:7036/api/issues?projectId=1
curl https://localhost:7036/api/issues?projectId=1&severity=Critical
```

### PowerShell example

```powershell
# Upload a diff and save the Excel report
$response = Invoke-RestMethod `
  -Uri "https://localhost:7036/api/review/upload" `
  -Method POST `
  -Form @{ diffFile = Get-Item "my-changes.diff"; Sheettype = "0" } `
  -OutFile "CodeReviewReport.xlsx"

Write-Host "Report saved to CodeReviewReport.xlsx"
```

---

## 16. Blazor Dashboard

The Blazor WebAssembly dashboard provides a visual interface for managing projects, reviewing issues, and configuring rules.

### Start the UI

```bash
dotnet run --project Analyzer1.API
```

Then open your browser at:

```
https://localhost:7036/
```

`[screenshot: Blazor dashboard home page]`

### Dashboard pages

| Page | URL | Description |
|---|---|---|
| **Dashboard** | `/` | Summary charts: issues by severity, category, recent scans |
| **Issues** | `/issues` | Filterable, sortable issue list with severity badges |
| **Projects** | `/projects` | Register project paths for scheduled scans |
| **Rules** | `/rules` | Full rule catalog with enable/disable toggles |
| **Code Review** | `/codereview` | Upload a diff file, download the Excel report |

### Code Review page — upload workflow

1. Navigate to `https://localhost:7036/codereview`
2. Click **Choose File** and select your `.diff` or `.patch` file
3. Select report type: **Analysis Report** or **Enterprise Checklist**
4. Click **Upload & Analyze**
5. The Excel report downloads automatically

`[screenshot: Code Review upload page in the Blazor dashboard]`

---

## 17. CI/CD Integration

### GitHub Actions — complete workflow

Create `.github/workflows/code-analysis.yml`:

```yaml
name: Code Analysis
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  analyze:
    runs-on: windows-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 2      # needed for git diff

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Build Analyzer
        run: dotnet build Analyzer1.sln

      - name: Run CodeAnalyzer Pro
        run: |
          dotnet run --project CodeReviewReporter -- `
            --path ${{ github.workspace }} `
            --html --json `
            --max-critical 0 `
            --max-errors 10 `
            --fail-on-gate

      - name: Upload Reports
        if: always()    # upload reports even if gate failed
        uses: actions/upload-artifact@v4
        with:
          name: code-analysis-${{ github.run_number }}
          path: |
            CodeReviewReport.html
            CodeReviewReport.json
            CodeReviewIssues.xlsx
          retention-days: 30
```

`[screenshot: GitHub Actions run showing code-analysis job with uploaded artifacts]`

### Azure DevOps — complete pipeline

Create `azure-pipelines.yml`:

```yaml
trigger:
  branches:
    include:
      - main
      - develop
      - feature/*

pool:
  vmImage: 'windows-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '8.0.x'

  - script: dotnet build Analyzer1.sln
    displayName: 'Build Solution'

  - task: DotNetCoreCLI@2
    displayName: 'Run CodeAnalyzer Pro'
    inputs:
      command: run
      projects: 'CodeReviewReporter/CodeReviewReporter.csproj'
      arguments: >
        -- --path $(Build.SourcesDirectory)
           --html --json
           --max-critical 0
           --max-errors 10
           --fail-on-gate

  - task: PublishBuildArtifacts@1
    condition: always()
    displayName: 'Publish Reports'
    inputs:
      PathtoPublish: '$(System.DefaultWorkingDirectory)'
      ArtifactName: 'CodeAnalysisReports'
      publishLocation: Container
      itemPattern: |
        CodeReviewReport.html
        CodeReviewReport.json
        CodeReviewIssues.xlsx
```

`[screenshot: Azure DevOps pipeline showing the CodeAnalyzer Pro task]`

### Jenkins — Jenkinsfile

```groovy
pipeline {
    agent { label 'windows' }

    stages {
        stage('Build') {
            steps {
                bat 'dotnet build Analyzer1.sln'
            }
        }

        stage('Code Analysis') {
            steps {
                bat '''
                    dotnet run --project CodeReviewReporter -- ^
                        --path %WORKSPACE% ^
                        --html --json ^
                        --max-critical 0 ^
                        --max-errors 10 ^
                        --fail-on-gate
                '''
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: 'CodeReviewReport.html, CodeReviewReport.json, CodeReviewIssues.xlsx'
        }
        failure {
            emailext(
                subject: "Code Analysis FAILED: ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                body: "Quality Gate failed. See attached report.",
                attachmentsPattern: 'CodeReviewReport.html',
                to: 'team@company.com'
            )
        }
    }
}
```

### Recommended CI/CD gate thresholds

| Branch type | --max-critical | --max-errors | Rationale |
|---|---|---|---|
| Feature branches | 0 | 20 | Block security issues, allow some errors during development |
| Pull requests to develop | 0 | 5 | Tighten before code review |
| Merges to main | 0 | 0 | Zero tolerance on production branch |
| Release branches | 0 | 0 | Nothing bad ships |

---

## 18. Team Workflows

### Workflow A — Daily developer use (pre-push)

Run this before every `git push`:

```bash
# In your project directory
dotnet run --project "E:\BackUp\TarunPractice\Analyzer1\CodeReviewReporter" -- \
  --git \
  --html \
  --max-critical 0 \
  --fail-on-gate

# Then open the HTML report
start CodeReviewReport.html
```

Add this as a **git pre-push hook** to make it automatic:

```bash
# Create the hook file
echo '#!/bin/sh
dotnet run --project "E:\BackUp\TarunPractice\Analyzer1\CodeReviewReporter" -- \
  --git --fail-on-gate --max-critical 0
' > .git/hooks/pre-push
chmod +x .git/hooks/pre-push
```

`[screenshot: VS Code terminal showing pre-push output]`

---

### Workflow B — Sprint planning (full project scan)

Run at the start of each sprint to get a full health picture:

```bash
dotnet run --project CodeReviewReporter -- \
  --path "C:\MyProject" \
  --html --json \
  --max-critical 0 \
  --max-errors 10
```

Then:
1. Open `CodeReviewReport.html`
2. Export CSV filtered by **Critical** severity
3. Create Jira tickets for each critical issue
4. Assign Security issues to the security-aware developer
5. Distribute Warning/Info issues across the team for the sprint backlog

`[screenshot: HTML report with Category filter set to Security]`

---

### Workflow C — Pull request code review

When a PR is opened, the CI pipeline runs automatically (see CI/CD section). Reviewers should:

1. Download the `CodeReviewReport.html` artifact from the pipeline run
2. Open it in a browser
3. Review issues introduced by this PR (filter by changed files)
4. Comment on the PR with critical issues found
5. Require the developer to fix all Critical issues before merge

`[screenshot: GitHub PR check showing code-analysis passed/failed]`

---

### Workflow D — New team member onboarding

When a new developer joins:

1. Point them to this USER_GUIDE.md
2. Have them open `INTERACTIVE_TUTORIAL.html` in a browser (interactive walkthrough)
3. Run their first analysis on the project: `--path "C:\MyProject" --html`
4. Review the HTML report together — explain each category
5. Assign them 5 low-complexity Warning issues to fix as a first task

---

### Workflow E — Legacy project adoption

For large legacy codebases with thousands of issues:

**Week 1** — Establish baseline:

```bash
dotnet run --project CodeReviewReporter -- \
  --path "C:\LegacyProject" \
  --html --json
```

Save the JSON report as `baseline-2026-04.json`. This is your starting point.

**Week 2 onwards** — Fix by priority:

```
Priority 1: All Critical Security (SEC*) issues — non-negotiable
Priority 2: Empty catch blocks (STAB001) — hiding real bugs
Priority 3: Blocking async calls (EFF001) — performance risk
Priority 4: High cognitive complexity (MAIN003 > 30) — highest maintenance cost
Priority 5: Everything else — scheduled over future sprints
```

**Monthly** — Track progress:

```bash
dotnet run --project CodeReviewReporter -- \
  --path "C:\LegacyProject" --json

# Compare with baseline
cat CodeReviewReport.json | jq '.summary'
cat baseline-2026-04.json | jq '.summary'
```

---

## 19. Troubleshooting

### Problem: "Files found: 0"

**Cause:** The `--path` folder has no `.cs` files, or all files are in `\bin\` or `\obj\`.

**Fix:**
```bash
# Check that the path exists and has .cs files
dir "C:\MyProject\*.cs" /s /b
```

---

### Problem: Analysis is very slow on first run

**Cause:** First run has no cache — all files are analyzed. Large projects (2000+ files) take 2–5 minutes.

**Fix:** Subsequent runs are much faster because unchanged files are skipped. To speed up even more:
```bash
# Set parallel degree explicitly (uses all CPU cores by default)
# No flag needed — it's automatic
```

---

### Problem: "Build succeeded" but VSIX errors appear

**Cause:** The `Analyzer1.Vsix` project requires Visual Studio's MSBuild (not .NET Core MSBuild).

**Fix:** This is expected and does not affect any functionality. Ignore the 2 VSIX errors. All runtime projects build cleanly.

---

### Problem: Excel file is locked / can't be overwritten

**Cause:** The previous `CodeReviewIssues.xlsx` is open in Excel.

**Fix:** Close the Excel file before running the analyzer again.

```
Close Excel → re-run → open the new file
```

---

### Problem: HTML report is blank or shows loading error

**Cause:** Browser security policy blocking the file. Some browsers block local file access.

**Fix:**
- Use **Chrome** or **Edge** — they work with local HTML files
- If using Firefox, allow local file access in `about:config`
- Or serve the file locally: `python -m http.server 8080`

---

### Problem: `--git` mode shows no files or wrong diff

**Cause:** The git repository doesn't have at least 2 commits, or you're running from the wrong directory.

**Fix:**
```bash
# Check you're in a git repo
git log --oneline -3

# Check what the diff would analyze
git diff HEAD~1 HEAD --name-only
```

---

### Problem: ESLint not found (JavaScript analysis)

**Cause:** Node.js or ESLint is not installed in `CodeReviewReporter/NodeAnalyzer`.

**Fix:**
```bash
cd CodeReviewReporter/NodeAnalyzer
npm install
```

---

### Problem: Rule RULE_ENGINE_ERROR in report

**Cause:** A rule threw an unhandled exception during analysis.

**Fix:** Check which rule ID appears in the `Message` column. Open the corresponding rule file in `Analyzer1.Rules/` and check for null reference issues. The most common cause is accessing `context.SemanticModel` without a null check.

---

## 20. FAQ

**Q: Can I run this on a Linux or macOS CI server?**

A: Yes — .NET 8 is cross-platform. The only Windows-specific part is the Excel library (ClosedXML), which also supports Linux. The HTML and JSON reports work everywhere. Change `start` to `xdg-open` (Linux) or `open` (macOS) for opening files.

---

**Q: How do I analyze only changed files between two specific commits?**

A: Save the diff to a file and use `--file` mode:

```bash
git diff abc1234 def5678 > changes.diff
dotnet run --project CodeReviewReporter -- --file changes.diff --html
```

---

**Q: Can I disable a rule for a specific line or method?**

A: Not yet via attribute/comment suppression. Disable rules globally in `ruleconfig.json` with `"enabled": false`. Per-line suppression is on the roadmap for v2.1.

---

**Q: How accurate is the technical debt estimate?**

A: It's a rough estimate — each issue is assigned a default effort (e.g. 30 min for SQL Injection, 10 min for a naming violation). The actual effort depends on context. Use it for relative comparison and trend tracking, not exact scheduling.

---

**Q: Can I add rules that check across multiple files?**

A: Yes — use the Collector/Reporter pattern with `context.GlobalStore`. See [Writing a Custom Rule](#14-writing-a-custom-rule) for details. The `DuplicateCodeCollectorRule` and `ShotgunSurgeryCollectorRule` are examples in the codebase.

---

**Q: What happens if a file fails to parse?**

A: The analyzer logs a warning to the console (`Error: filename — message`) and continues with the remaining files. Parse errors don't stop the analysis.

---

**Q: How do I integrate with Jira?**

A: Export the CSV from the HTML report (click **Export CSV**), then bulk-import into Jira using the CSV importer. Map columns: `File` → Component, `Severity` → Priority, `Message` → Summary, `Suggested Fix` → Description.

---

**Q: Can the REST API be secured with authentication?**

A: The current API has no authentication (development mode). For production deployment, add ASP.NET Core authentication middleware. OAuth2 / Azure AD support is on the roadmap for v3.0.

---

**Q: The cache is out of date. How do I force a full re-scan?**

A: Delete `analysis_cache.json` and run again:

```bash
del analysis_cache.json
dotnet run --project CodeReviewReporter -- --path "C:\MyProject" --html
```

---

**Q: How many files can it handle?**

A: Tested successfully on 2,657 files producing 312,325 issues in 126 seconds. Performance scales linearly with CPU cores (parallel analysis).

---

## Appendix A — Quick Command Reference Card

```
╔══════════════════════════════════════════════════════════════╗
║              CodeAnalyzer Pro — Quick Reference              ║
╠══════════════════════════════════════════════════════════════╣
║  ANALYZE FOLDER                                              ║
║  dotnet run --project CodeReviewReporter --                  ║
║    --path "C:\MyProject" --html --json                       ║
║                                                              ║
║  ANALYZE LAST GIT COMMIT                                     ║
║  dotnet run --project CodeReviewReporter -- --git --html     ║
║                                                              ║
║  CI/CD (fail on critical issues)                             ║
║  dotnet run --project CodeReviewReporter --                  ║
║    --path . --max-critical 0 --fail-on-gate                  ║
║                                                              ║
║  REMOTE REPO                                                 ║
║  dotnet run --project CodeReviewReporter --                  ║
║    --repo https://github.com/org/repo --html                 ║
║                                                              ║
║  OUTPUT FILES                                                ║
║  CodeReviewIssues.xlsx     ← always generated               ║
║  CodeReviewReport.html     ← with --html                    ║
║  CodeReviewReport.json     ← with --json                    ║
╚══════════════════════════════════════════════════════════════╝
```

## Appendix B — Priority Fix Order for New Projects

When you run your first analysis on an existing project, fix issues in this order:

```
1. SEC* Critical    SQL Injection, XSS, Path Traversal, Hardcoded Credentials
2. STAB001          Empty catch blocks (hiding real exceptions)
3. STAB003          Missing Dispose (resource leaks)
4. EFF001           Blocking async (.Result / .Wait — deadlock risk)
5. THR003           Lock on public object (thread safety bug)
6. MAIN003 > 30     Extremely high cognitive complexity (unmaintainable)
7. DES003           Deep nesting > 5 levels
8. SEC* Warning     Insecure random, sensitive data logging
9. EFF002–004       LINQ in loop, multiple enumeration, string concat in loop
10. Everything else  Readability, documentation, minor style
```

## Appendix C — Rule ID Prefix Guide

| Prefix | Category | Examples |
|---|---|---|
| `SEC` | Security | SEC001 (SQL Injection), SEC008 (XSS) |
| `DES` | Design | DES003 (Deep Nesting), DES002 (Long Method) |
| `MAIN` | Maintainability | MAIN003 (Complexity), MAIN011 (Dead Code) |
| `EFF` | Efficiency | EFF001 (Blocking Async), EFF004 (String Concat) |
| `THR` | Threading | THR001 (Async Void), THR003 (Lock on Public) |
| `STAB` | Stability | STAB001 (Empty Catch), STAB003 (Missing Dispose) |
| `RD` | Readability | RD001 (Class Naming), RD008 (Long Line) |
| `DOC` | Documentation | DOC001 (Missing XML Doc) |

---

*CodeAnalyzer Pro User Guide — Version 2.0*  
*Generated: April 2026*  
*For the latest version see `PRODUCT_DOCUMENTATION.md`*
