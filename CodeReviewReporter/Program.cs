using CodeReviewReporter.Models.CodeReviewReporter.Models;
using CodeReviewReporter.Services;
using Microsoft.Build.Locator;
using System.Diagnostics;

class Program
{


    static async Task Main(string[] args)
    {
        //if (args.Length < 1)
        //{
        //    Console.WriteLine("Usage: CodeReviewReporter.exe <DiffFilePath>");
        //    return;
        //}

        string diffFilePath = args[0];// args[0];

        if (!File.Exists(diffFilePath))
        {
            Console.WriteLine("Diff file not found.");
            return;
        }

        try
        {
            string diffContent = "";
            Console.WriteLine("Reading diff file...");
            if (args.Contains("--git"))
                diffContent = GetGitDiff();
            else if (args.Contains("--svn"))
                diffContent = GetSvnDiff();
            else if (args.Contains("--file"))
                diffContent = File.ReadAllText(args[1]);

            

            var analyzer = new SvnCommitAnalyzer();
            var files = analyzer.ExtractCodeFromDiff(diffContent);

          //  var changes = analyzer.ParseDiff(diffContent);
         //   string codeToAnalyze = analyzer.ExtractAddedCode(diffContent);


            var analyzerService = new RoslynAnalyzerService();

            var roslyn = new RoslynAnalyzerService();
            var issues = new List<CodeReviewIssue>();

            // var issues = await roslyn.AnalyzeCode(codeToAnalyze, "CommitCode.cs");
            foreach (var file in files)
            {
                var result = await analyzerService.AnalyzeCode(file.file, file.code);
                issues.AddRange(result);
            }
            var reportService = new ExcelReportService();

            string outputFile = "CodeReviewIssues.xlsx";

            reportService.GenerateIssueReport(issues, outputFile);
            var rs=new EnterpriseCodeReviewExcel();
            string outputFile1 = "EnterpriseCodeReviewExcel.xlsx";
            rs.GenerateFullChecklist(outputFile1);

            //  var report = new ExcelReportService();

            ////   report.GenerateReviewReport(issues);
            //   var reportService = new ExcelReportService();

            //   string outputFile = "CommitCodeReview.xlsx";

            //   reportService.GenerateCommitReport(changes, outputFile);

            Console.WriteLine("Code Review Excel generated successfully.");
          //  Console.WriteLine("Output File : " + Path.GetFullPath(outputFile));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error : " + ex.Message);
        }
    }
    public static string GetSvnDiff()
    {
        var process = new Process();
        process.StartInfo.FileName = "svn";
        process.StartInfo.Arguments = "diff";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;

        process.Start();
        string diff = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return diff;
    }


    public static string GetGitDiff()
    {
        var process = new Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = "diff";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;

        process.Start();
        string diff = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return diff;
    }

    //static async Task Main(string[] args)
    //{
    //    MSBuildLocator.RegisterDefaults();

    //    //string solutionPath = @" E:\BackUp\TarunPractice\MTenantSolution\MTenantSolution\MTenantSolution.sln";
    //    string solutionPath = @"E:\BackUp\TarunPractice\MTenantSolution\MTenantSolution\MTenantSolution.sln";

    //    // Check if solution file exists
    //    if (!File.Exists(solutionPath))
    //    {
    //        Console.WriteLine("Solution file not found: " + solutionPath);
    //        return;
    //    }


    //    var analyzerService = new AnalyzerService();

    //    var issues = await analyzerService.AnalyzeSolution(solutionPath);

    //    var reportService = new ExcelReportService();

    //    reportService.GenerateReport(issues);

    //    Console.WriteLine("Excel code review report generated.");
    //}
}