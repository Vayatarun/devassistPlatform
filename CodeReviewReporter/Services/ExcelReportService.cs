using ClosedXML.Excel;
using CodeReviewReporter.Models;
using CodeReviewReporter.Models.CodeReviewReporter.Models;
using System.Collections.Generic;
using System.IO;

namespace CodeReviewReporter.Services
{
    public class ExcelReportService
    {
        //public void GenerateReport(List<CodeReviewIssue> issues, string outputPath = "CodeReviewReport.xlsx")
        //{
        //    using var workbook = new XLWorkbook();
        //    var sheet = workbook.Worksheets.Add("Code Review");

        //    // Headers
        //    sheet.Cell(1, 1).Value = "File";
        //    sheet.Cell(1, 2).Value = "Line";
        //    sheet.Cell(1, 3).Value = "Rule";
        //    sheet.Cell(1, 4).Value = "Category";
        //    sheet.Cell(1, 5).Value = "Message";
        //    sheet.Cell(1, 6).Value = "Severity";

        //    // Fill data
        //    for (int i = 0; i < issues.Count; i++)
        //    {
        //        var issue = issues[i];
        //        int row = i + 2;

        //        sheet.Cell(row, 1).Value = issue.FilePath;
        //        sheet.Cell(row, 2).Value = issue.Line;
        //        sheet.Cell(row, 3).Value = issue.RuleId;
        //        sheet.Cell(row, 4).Value = issue.Category;
        //        sheet.Cell(row, 5).Value = issue.Message;
        //        sheet.Cell(row, 6).Value = issue.Severity;
        //    }

        //    sheet.Columns().AdjustToContents();
        //    workbook.SaveAs(outputPath);
        //}


        public void GenerateIssueReportToStream(List<CodeReviewIssue> issues, Stream stream)
        {
            using var workbook = new XLWorkbook();

            var sheet = workbook.Worksheets.Add("Code Review");

            sheet.Cell(1, 1).Value = "File";
            sheet.Cell(1, 2).Value = "Line";
            sheet.Cell(1, 3).Value = "Rule";
            sheet.Cell(1, 4).Value = "Severity";
            sheet.Cell(1, 5).Value = "Message";
            sheet.Cell(1, 6).Value = "Code";
            sheet.Cell(1, 7).Value = "Suggested Fix";

            int row = 2;

            foreach (var issue in issues)
            {
                sheet.Cell(row, 1).Value = issue.FileName;
                sheet.Cell(row, 2).Value = issue.LineNumber;
                sheet.Cell(row, 3).Value = issue.RuleId;
                sheet.Cell(row, 4).Value = issue.Severity;
                sheet.Cell(row, 5).Value = issue.Message;
                sheet.Cell(row, 6).Value = issue.Code;
                sheet.Cell(row, 7).Value = issue.SuggestedFix;

                row++;
            }

            sheet.Columns().AdjustToContents();

            workbook.SaveAs(stream);
        }


        public void GenerateCommitReport(List<CommitChange> changes, string outputPath = "CommitCodeReview.xlsx")
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Commit Review");

                // Header
                sheet.Cell(1, 1).Value = "File";
                sheet.Cell(1, 2).Value = "Line";
                sheet.Cell(1, 3).Value = "Code";
                sheet.Cell(1, 4).Value = "Change Type";

                // Header Style
                sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
                sheet.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;

                foreach (var change in changes)
                {
                    sheet.Cell(row, 1).Value = change.FileName;
                    sheet.Cell(row, 2).Value = change.LineNumber;
                    sheet.Cell(row, 3).Value = change.Code;
                    sheet.Cell(row, 4).Value = change.ChangeType;

                    // Highlight rows
                    if (change.ChangeType == "Added")
                    {
                        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    }
                    else if (change.ChangeType == "Deleted")
                    {
                        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
                    }
                    else if (change.ChangeType == "Modified")
                    {
                        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    }

                    row++;
                }

                sheet.Columns().AdjustToContents();

                workbook.SaveAs(outputPath);

                Console.WriteLine("Excel report saved: " + Path.GetFullPath(outputPath));
            }
        }

        //public void GenerateIssueReport(List<CodeReviewIssue> issues, string outputPath = "CodeReviewIssues.xlsx")
        //{
        //    using (var workbook = new XLWorkbook())
        //    {
        //        var sheet = workbook.Worksheets.Add("Analyzer Issues");

        //        // Headers
        //        sheet.Cell(1, 1).Value = "File";
        //        sheet.Cell(1, 2).Value = "Line";
        //        sheet.Cell(1, 3).Value = "Rule Id";
        //        sheet.Cell(1, 4).Value = "Severity";
        //        sheet.Cell(1, 5).Value = "Message";
        //        sheet.Cell(1, 6).Value = "Code";

        //        // Header Style
        //        var headerRange = sheet.Range(1, 1, 1, 6);
        //        headerRange.Style.Font.Bold = true;
        //        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        //        int row = 2;

        //        foreach (var issue in issues)
        //        {
        //            sheet.Cell(row, 1).Value = issue.FileName;
        //            sheet.Cell(row, 2).Value = issue.LineNumber;
        //            sheet.Cell(row, 3).Value = issue.RuleId;
        //            sheet.Cell(row, 4).Value = issue.Severity;
        //            sheet.Cell(row, 5).Value = issue.Message;
        //            sheet.Cell(row, 6).Value = issue.Code;

        //            // Highlight severity
        //            if (issue.Severity == "Error")
        //                sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;

        //            if (issue.Severity == "Warning")
        //                sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;

        //            if (issue.Severity == "Info")
        //                sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightBlue;

        //            row++;
        //        }

        //        sheet.Columns().AdjustToContents();

        //        workbook.SaveAs(outputPath);

        //        Console.WriteLine("Excel report generated: " + Path.GetFullPath(outputPath));
        //    }
        //}


        public void GenerateIssueReport(List<CodeReviewIssue> issues, string outputPath = "CodeReviewIssues.xlsx")
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Code Review");

                // Headers
                string[] headers =
                {
                "File",
                "Line",
                "Rule Id",
                "Category",
                "Severity",
                "Message",
                "Code",
                "Suggested Fix",
                "Reviewer Comment",
                "Status"
            };

                for (int i = 0; i < headers.Length; i++)
                    sheet.Cell(1, i + 1).Value = headers[i];

                // Header style
                var headerRange = sheet.Range(1, 1, 1, headers.Length);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;

                foreach (var issue in issues)
                {
                    sheet.Cell(row, 1).Value = issue.FileName;
                    sheet.Cell(row, 2).Value = issue.LineNumber;
                    sheet.Cell(row, 3).Value = issue.RuleId;
                    sheet.Cell(row, 4).Value = GetCategory(issue.RuleId);
                    sheet.Cell(row, 5).Value = issue.Severity;
                    sheet.Cell(row, 6).Value = issue.Message;
                    sheet.Cell(row, 7).Value = issue.Code;

                    sheet.Cell(row, 8).Value = issue.SuggestedFix; // Suggested fix
                    sheet.Cell(row, 9).Value = issue.Description; // Reviewer comment
                    sheet.Cell(row, 10).Value = "Open";

                    // Severity coloring
                    if (issue.Severity == "Error")
                        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;

                    if (issue.Severity == "Warning")
                        sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;

                    row++;
                }

                // Freeze header
                sheet.SheetView.FreezeRows(1);

                // Add filters
                sheet.RangeUsed().SetAutoFilter();

                // Adjust width
                sheet.Columns().AdjustToContents();

                workbook.SaveAs(outputPath);

                Console.WriteLine("Excel report generated: " + Path.GetFullPath(outputPath));
            }
        }

        private string GetCategory(string ruleId)
        {
            if (ruleId.StartsWith("DB"))
                return "Database";

            if (ruleId.StartsWith("EF"))
                return "Entity Framework";

            if (ruleId.StartsWith("EX"))
                return "Exception Handling";

            if (ruleId.StartsWith("LP"))
                return "Loop Optimization";

            if (ruleId.StartsWith("RD"))
                return "Readability";

            return "General";
        }






    }
}



