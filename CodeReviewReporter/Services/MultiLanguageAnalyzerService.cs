using Analyzer1;
using Analyzer1.CodeFixes;
using CodeReviewReporter.Models.CodeReviewReporter.Models;
using DocumentFormat.OpenXml.InkML;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

public class MultiLanguageAnalyzerService
{
    #region Public Entry Point

    public async Task<List<CodeReviewIssue>> AnalyzeCode(string fileName, string code)
    {
        var extension = Path.GetExtension(fileName).ToLower();

        switch (extension)
        {
            case ".cs":
                return await AnalyzeCSharp(fileName, code);

            case ".js":
                return await AnalyzeJavaScript(fileName, code);

            case ".cshtml":
                return await AnalyzeCsHtmlJavaScript(fileName, code);
            default:
                return new List<CodeReviewIssue>(); // Ignore unsupported files
        }
    }

    #endregion

    #region C# ANALYSIS (Roslyn)

    private ImmutableArray<DiagnosticAnalyzer> GetAnalyzers()
    {
        return ImmutableArray.Create<DiagnosticAnalyzer>(
            new DatabaseAnalyzer(),
            new EFAnalyzer(),
            new ExceptionAnalyzer(),
            new LoopAnalyzer(),
            new ReadabilityAnalyzer()
        );
    }

    private ImmutableArray<CodeFixProvider> GetCodeFixProviders()
    {
        return ImmutableArray.Create<CodeFixProvider>(
            new DatabaseCodeFixProvider(),
            new EFCodeFixProvider(),
            new ExceptionCodeFixProvider(),
            new LoopCodeFixProvider(),
            new ReadabilityCodeFixProvider()
        );
    }

    private async Task<List<CodeReviewIssue>> AnalyzeCSharp(string fileName, string code)
    {
        var issues = new List<CodeReviewIssue>();

        var tree = CSharpSyntaxTree.ParseText(code);

        var compilation = CSharpCompilation.Create("Analysis")
            .AddSyntaxTrees(tree)
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var analyzers = GetAnalyzers();

        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        var document = new AdhocWorkspace()
            .AddProject("AnalysisProject", LanguageNames.CSharp)
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddDocument(fileName, code);

        var codeFixProviders = GetCodeFixProviders();

        var codeLines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var diagnostic in diagnostics)
        {
            var lineSpan = diagnostic.Location.GetLineSpan();
            int line = lineSpan.StartLinePosition.Line + 1;

            string codeLine = codeLines.ElementAtOrDefault(line - 1) ?? "";
            string suggestedFix = "";

            foreach (var provider in codeFixProviders)
            {
                if (provider.FixableDiagnosticIds.Contains(diagnostic.Id))
                {
                    var actions = new List<CodeAction>();

                    var context = new CodeFixContext(
                        document,
                        diagnostic,
                        (a, d) => actions.Add(a),
                        CancellationToken.None);

                    await provider.RegisterCodeFixesAsync(context);

                    if (actions.Any())
                    {
                        suggestedFix = actions.First().Title;
                        break;
                    }
                }
            }

            issues.Add(new CodeReviewIssue
            {
                FileName = fileName,
                LineNumber = line,
                RuleId = diagnostic.Id,
                Severity = diagnostic.Severity.ToString(),
                Message = diagnostic.GetMessage(),
                Description = diagnostic.Descriptor.Description.ToString(),
                Code = codeLine,
                SuggestedFix = suggestedFix
            });
        }

        return issues;
    }

    #endregion

    #region JavaScript ANALYSIS (ESLint)


  
    private async Task<List<CodeReviewIssue>> AnalyzeJavaScript(string fileName, string code)
    {
        var issues = new List<CodeReviewIssue>();

        // 1️⃣ Create temporary JS file
      

      

        // 2️⃣ Locate NodeAnalyzer folder
        var nodeAnalyzerPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "NodeAnalyzer"
        );

        var tempFile = Path.Combine(nodeAnalyzerPath, Guid.NewGuid() + ".js");

        await File.WriteAllTextAsync(tempFile, code);
        var eslintPath = Path.Combine(
            nodeAnalyzerPath,
            "node_modules",
            "eslint",
            "bin",
            "eslint.js"
        );

        var configPath = Path.Combine(nodeAnalyzerPath, "eslint.config.cjs");

        // 3️⃣ Safety checks
        if (!File.Exists(eslintPath))
            throw new FileNotFoundException("ESLint not found. Run npm install inside NodeAnalyzer.");

        if (!File.Exists(configPath))
            throw new FileNotFoundException("eslint.config.js not found in NodeAnalyzer folder.");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "node",
            Arguments = $"\"{eslintPath}\" \"{tempFile}\" -f json --config \"{configPath}\"",
            WorkingDirectory = nodeAnalyzerPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };

        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        Console.WriteLine("Exit Code: " + process.ExitCode);

        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.WriteLine("ESLint STDERR:");
            Console.WriteLine(error);
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            // No lint issues OR something wrong
            return issues;
        }

        try
        {
            var eslintResults = JsonSerializer.Deserialize<List<EslintResult>>(output);

            if (eslintResults == null)
                return issues;

            foreach (var result in eslintResults)
            {
                foreach (var message in result.messages)
                {
                    issues.Add(new CodeReviewIssue
                    {
                        FileName = fileName,
                        LineNumber = message.line,
                        RuleId = message.ruleId ?? "JS_RULE",
                        Severity = message.severity == 2 ? "Error" : "Warning",
                        Message = message.message,
                        Description = "ESLint JavaScript Rule",
                        Code = "",
                        SuggestedFix = message.ruleId
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("JSON Parse Error: " + ex.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }

        return issues;
    }
    private class EslintResult
    {
        public string filePath { get; set; }
        public List<EslintMessage> messages { get; set; }
    }

    private class EslintMessage
    {
        public int line { get; set; }
        public string ruleId { get; set; }
        public string message { get; set; }
        public int severity { get; set; }
    }

    #endregion


    private async Task<List<CodeReviewIssue>> AnalyzeCsHtmlJavaScript(string fileName, string cshtmlContent)
    {
        var issues = new List<CodeReviewIssue>();
        var razorIssues = AnalyzeRazorContent(fileName, cshtmlContent);
        issues.AddRange(razorIssues);
        var scriptMatches = Regex.Matches(
            cshtmlContent,
            @"<script\b([^>]*)>([\s\S]*?)<\/script>",
            RegexOptions.IgnoreCase
        );

        if (scriptMatches.Count == 0)
            return issues;

        var nodeAnalyzerPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "NodeAnalyzer"
        );

        var eslintPath = Path.Combine(
            nodeAnalyzerPath,
            "node_modules",
            "eslint",
            "bin",
            "eslint.js"
        );
  

        var configPath = Path.Combine(nodeAnalyzerPath, "eslint.config.cjs");

        foreach (Match match in scriptMatches)
        {
            var attributes = match.Groups[1].Value;
            var jsCode = match.Groups[2].Value;

            if (string.IsNullOrWhiteSpace(jsCode))
                continue;

            // ❌ Skip external script files
            if (attributes.Contains("src=", StringComparison.OrdinalIgnoreCase))
                continue;

            // ❌ Skip template scripts
            if (attributes.Contains("text/html", StringComparison.OrdinalIgnoreCase) ||
                attributes.Contains("template", StringComparison.OrdinalIgnoreCase))
                continue;

            // ❌ Skip pure HTML blocks
            if (Regex.IsMatch(jsCode.TrimStart(), @"^<"))
                continue;

            // ❌ Skip handlebars / mustache templates
            if (jsCode.Contains("{{"))
                continue;

            // ❌ Skip if it doesn't look like real JS
            if (!Regex.IsMatch(jsCode, @"\b(function|var|let|const|\=\>|if\s*\(|for\s*\(|while\s*\()"))
                continue;

            // 🔥 Clean Razor syntax safely
            jsCode = Regex.Replace(jsCode, @"@\w+(\.\w+)*", "\"RAZOR_VALUE\"");
            jsCode = Regex.Replace(jsCode, @"@\{[\s\S]*?\}", "");
            jsCode = Regex.Replace(jsCode, @"@\w+\s*\(.*?\)\s*\{", "");

            // 🔥 Track original line number for mapping
            int scriptStartLine = cshtmlContent
                .Substring(0, match.Index)
                .Count(c => c == '\n') + 1;

            var tempFile = Path.Combine(nodeAnalyzerPath, Guid.NewGuid() + ".js");
            await File.WriteAllTextAsync(tempFile, jsCode);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"\"{eslintPath}\" \"{tempFile}\" -f json --config \"{configPath}\"",
                    WorkingDirectory = nodeAnalyzerPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine("ESLint Error: " + error);
            }

            if (!string.IsNullOrWhiteSpace(output))
            {
                try
                {
                    var eslintResults = JsonSerializer.Deserialize<List<EslintResult>>(output);

                    if (eslintResults != null)
                    {
                        foreach (var result in eslintResults)
                        {
                            foreach (var message in result.messages)
                            {
                                issues.Add(new CodeReviewIssue
                                {
                                    FileName = fileName,
                                    LineNumber = scriptStartLine + message.line - 1,
                                    RuleId = message.ruleId ?? "JS_RULE",
                                    Severity = message.severity == 2 ? "Error" : "Warning",
                                    Message = message.message,
                                    Description = "ESLint JavaScript Rule (from .cshtml)",
                                    SuggestedFix = message.ruleId
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("JSON Parse Error: " + ex.Message);
                }
            }

            File.Delete(tempFile);
        }

        return issues;
    }

    private List<CodeReviewIssue> AnalyzeRazorContent(string fileName, string content)
    {
        var issues = new List<CodeReviewIssue>();

        var pattern = @"@Url\.Content\(\s*""\s+~\/";

        var matches = Regex.Matches(content, pattern);

        foreach (Match match in matches)
        {
            int lineNumber = content.Substring(0, match.Index)
                                     .Count(c => c == '\n') + 1;

            issues.Add(new CodeReviewIssue
            {
                FileName = fileName,
                LineNumber = lineNumber,
                RuleId = "Razor_RULE",
                Severity = "Error",
                Message = "Space detected before ~/ inside @Url.Content(). Remove the space."
            });
        }

        return issues;
    }


}