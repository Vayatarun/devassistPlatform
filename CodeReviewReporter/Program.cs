using CodeReviewReporter.Models.CodeReviewReporter.Models;
using CodeReviewReporter.Services;
using System.Diagnostics;
using System.Text.Json;

class Program
{
    static string CacheFile = "analysis_cache.json";

    static async Task<int> Main(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            PrintUsage();
            return 0;
        }

        bool generateHtml  = args.Contains("--html");
        bool generateJson  = args.Contains("--json");
        bool failOnGate    = args.Contains("--fail-on-gate");
        int  maxCritical   = GetArgInt(args, "--max-critical", 0);
        int  maxErrors     = GetArgInt(args, "--max-errors", 5);

        var analyzerService = new RoslynAnalyzerService();
        var issues = new List<CodeReviewIssue>();
        var cache  = LoadCache();
        var sw     = Stopwatch.StartNew();

        try
        {
            Console.WriteLine("=== CodeAnalyzer Pro ===");
            Console.WriteLine($"Started: {DateTime.Now:HH:mm:ss}");
            Console.WriteLine();

            if (args.Contains("--git"))
            {
                Console.WriteLine("Mode: Git diff analysis");
                string diff = GetGitDiff();
                await AnalyzeDiff(diff, analyzerService, issues);
            }
            else if (args.Contains("--svn"))
            {
                Console.WriteLine("Mode: SVN diff analysis");
                string diff = GetSvnDiff();
                await AnalyzeDiff(diff, analyzerService, issues);
            }
            else if (args.Contains("--file"))
            {
                string filePath = GetArg(args, "--file") ?? args[1];
                Console.WriteLine($"Mode: File diff — {filePath}");
                string diff = File.ReadAllText(filePath);
                await AnalyzeDiff(diff, analyzerService, issues);
            }
            else if (args.Contains("--repo"))
            {
                string repoUrl = GetArg(args, "--repo") ?? args[1];
                Console.WriteLine($"Mode: Repository — {repoUrl}");
                string folder = CloneRepo(repoUrl);
                await AnalyzeFolder(folder, analyzerService, issues, cache);
            }
            else if (args.Contains("--path"))
            {
                string folder = GetArg(args, "--path") ?? args[1];
                Console.WriteLine($"Mode: Folder — {folder}");
                await AnalyzeFolder(folder, analyzerService, issues, cache);
            }
            else
            {
                PrintUsage();
                return 0;
            }

            SaveCache(cache);
            sw.Stop();

            PrintSummary(issues, sw.Elapsed);

            // ── Quality Gate ───────────────────────────────────────────────
            var gate = EvaluateGate(issues, maxCritical, maxErrors);
            PrintGateResult(gate);

            // ── Reports ────────────────────────────────────────────────────
            new ExcelReportService().GenerateIssueReport(issues, "CodeReviewIssues.xlsx");

            if (generateHtml)
                new HtmlReportService().GenerateReport(issues, "CodeReviewReport.html", gate);

            if (generateJson)
                new JsonReportService().GenerateReport(issues, "CodeReviewReport.json");

            Console.WriteLine();
            Console.WriteLine("Done.");

            return (failOnGate && !gate.Passed) ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Fatal error: " + ex.Message);
            return 2;
        }
    }

    // ─── Quality Gate ──────────────────────────────────────────────────────

    static QualityGateSummary EvaluateGate(List<CodeReviewIssue> issues, int maxCritical, int maxErrors)
    {
        var gate = new QualityGateSummary { Passed = true };

        int critical = issues.Count(i => i.Severity?.Equals("Critical", StringComparison.OrdinalIgnoreCase) == true);
        int errors   = issues.Count(i => i.Severity?.Equals("Error",    StringComparison.OrdinalIgnoreCase) == true);

        if (critical > maxCritical)
        {
            gate.Passed = false;
            gate.FailureReasons.Add($"Critical issues: {critical} (max allowed: {maxCritical})");
        }
        if (errors > maxErrors)
        {
            gate.Passed = false;
            gate.FailureReasons.Add($"Error issues: {errors} (max allowed: {maxErrors})");
        }
        return gate;
    }

    static void PrintSummary(List<CodeReviewIssue> issues, TimeSpan elapsed)
    {
        Console.WriteLine();
        Console.WriteLine("─────────────────────────────────────────");
        Console.WriteLine($"  Analysis complete in {elapsed.TotalSeconds:F1}s");
        Console.WriteLine($"  Total issues  : {issues.Count}");
        Console.WriteLine($"  Critical      : {issues.Count(i => i.Severity == "Critical")}");
        Console.WriteLine($"  Error         : {issues.Count(i => i.Severity == "Error")}");
        Console.WriteLine($"  Major         : {issues.Count(i => i.Severity == "Major")}");
        Console.WriteLine($"  Warning       : {issues.Count(i => i.Severity == "Warning")}");
        Console.WriteLine($"  Info/Minor    : {issues.Count(i => i.Severity is "Info" or "Minor")}");
        Console.WriteLine();

        var byCategory = issues
            .GroupBy(i => string.IsNullOrEmpty(i.Category) ? "General" : i.Category)
            .OrderByDescending(g => g.Count());
        Console.WriteLine("  By category:");
        foreach (var g in byCategory)
            Console.WriteLine($"    {g.Key,-22}: {g.Count()}");

        int debtMins = issues.Count * 8;
        string debt = debtMins < 60 ? $"{debtMins}min" :
                      debtMins < 480 ? $"{debtMins / 60}h {debtMins % 60}min" :
                      $"{debtMins / 480}d {(debtMins % 480) / 60}h";
        Console.WriteLine($"  Technical debt: {debt} (estimated)");
        Console.WriteLine("─────────────────────────────────────────");
    }

    static void PrintGateResult(QualityGateSummary gate)
    {
        Console.WriteLine();
        if (gate.Passed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✔  Quality Gate: PASSED");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✘  Quality Gate: FAILED");
            foreach (var r in gate.FailureReasons)
                Console.WriteLine($"     · {r}");
        }
        Console.ResetColor();
        Console.WriteLine();
    }

    // ─── Folder analysis ──────────────────────────────────────────────────

    static async Task AnalyzeFolder(string folder, RoslynAnalyzerService analyzerService,
        List<CodeReviewIssue> issues, Dictionary<string, string> cache)
    {
        var files = GetAllCodeFiles(folder);
        Console.WriteLine($"Files found: {files.Count}");

        int maxParallel = Environment.ProcessorCount;
        var semaphore   = new SemaphoreSlim(maxParallel);
        int processed   = 0;

        var tasks = files.Select(async (file, index) =>
        {
            await semaphore.WaitAsync();
            try
            {
                if (new FileInfo(file).Length > 2_000_000)
                    return;

                string code = await File.ReadAllTextAsync(file);
                string hash = GetHash(code);

                if (cache.TryGetValue(file, out var cached) && cached == hash)
                    return;

                var result = await analyzerService.AnalyzeCode(file, code);
                lock (issues) { issues.AddRange(result); }
                lock (cache)  { cache[file] = hash; }

                int n = Interlocked.Increment(ref processed);
                if (n % 10 == 0 || n == files.Count)
                    Console.WriteLine($"  Processed {n}/{files.Count} files, {issues.Count} issues so far");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {file} — {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    // ─── Diff analysis ────────────────────────────────────────────────────

    static async Task AnalyzeDiff(string diffContent, RoslynAnalyzerService analyzerService, List<CodeReviewIssue> issues)
    {
        var parser   = new SvnCommitAnalyzer();
        var files    = parser.ExtractCodeFromDiff(diffContent);
        int maxParallel = Environment.ProcessorCount;
        var semaphore   = new SemaphoreSlim(maxParallel);

        var tasks = files.Select(async file =>
        {
            await semaphore.WaitAsync();
            try
            {
                var result = await analyzerService.AnalyzeCode(file.file, file.code);
                lock (issues) { issues.AddRange(result); }
            }
            finally { semaphore.Release(); }
        });

        await Task.WhenAll(tasks);
    }

    // ─── Cache ────────────────────────────────────────────────────────────

    static Dictionary<string, string> LoadCache()
    {
        if (!File.Exists(CacheFile)) return new();
        try
        {
            var json = File.ReadAllText(CacheFile);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch { return new(); }
    }

    static void SaveCache(Dictionary<string, string> cache)
    {
        var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(CacheFile, json);
    }

    static string GetHash(string content)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(bytes);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    static string CloneRepo(string repoUrl)
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone --depth 1 {repoUrl} \"{folder}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        }) ?? throw new InvalidOperationException("Failed to start git. Ensure git is on PATH.");

        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git clone failed (exit {process.ExitCode}).");
        return folder;
    }

    static List<string> GetAllCodeFiles(string path) =>
        Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
            .ToList();

    static string GetGitDiff()
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = "git", Arguments = "diff HEAD~1 HEAD",
            RedirectStandardOutput = true, UseShellExecute = false
        })!;
        return p.StandardOutput.ReadToEnd();
    }

    static string GetSvnDiff()
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = "svn", Arguments = "diff",
            RedirectStandardOutput = true, UseShellExecute = false
        })!;
        return p.StandardOutput.ReadToEnd();
    }

    static string? GetArg(string[] args, string flag)
    {
        int idx = Array.IndexOf(args, flag);
        return (idx >= 0 && idx + 1 < args.Length) ? args[idx + 1] : null;
    }

    static int GetArgInt(string[] args, string flag, int defaultValue)
    {
        var val = GetArg(args, flag);
        return val != null && int.TryParse(val, out int result) ? result : defaultValue;
    }

    static void PrintUsage()
    {
        Console.WriteLine(@"
CodeAnalyzer Pro — Static Code Analyzer
========================================
Usage:
  --path <folder>       Analyze all .cs files in a folder
  --git                 Analyze the latest git diff (HEAD~1..HEAD)
  --svn                 Analyze the current SVN diff
  --file <diff-file>    Analyze a saved diff file
  --repo <git-url>      Clone and analyze a repository

Output options:
  --html                Generate HTML report (CodeReviewReport.html)
  --json                Generate JSON report (CodeReviewReport.json)
                        Excel is always generated (CodeReviewIssues.xlsx)

Quality Gate:
  --max-critical <n>    Fail gate if critical issues exceed n (default 0)
  --max-errors   <n>    Fail gate if error issues exceed n   (default 5)
  --fail-on-gate        Return exit code 1 when gate fails (CI/CD)

Examples:
  dotnet run --project CodeReviewReporter -- --path C:\MyProject --html --json
  dotnet run --project CodeReviewReporter -- --git --fail-on-gate --max-critical 0
  dotnet run --project CodeReviewReporter -- --repo https://github.com/org/repo --html
");
    }
}
