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
                AddSummarySheet(workbook, issues);
                AddIssuesSheet(workbook, issues);

                workbook.SaveAs(outputPath);
                Console.WriteLine("Excel report generated: " + Path.GetFullPath(outputPath));
            }
        }

        private void AddSummarySheet(XLWorkbook workbook, List<CodeReviewIssue> issues)
        {
            var sheet = workbook.Worksheets.Add("Summary");
            sheet.TabColor = XLColor.DarkBlue;

            // Title
            sheet.Cell(1, 1).Value = "CodeAnalyzer Pro — Analysis Summary";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 16;
            sheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            sheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
            sheet.Range(1, 1, 1, 4).Merge();

            sheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            sheet.Cell(2, 1).Style.Font.Italic = true;
            sheet.Range(2, 1, 2, 4).Merge();

            // Severity summary
            int r = 4;
            WriteSummaryHeader(sheet, r++, 1, "Severity Breakdown");
            WriteSummaryRow(sheet, r++, 1, "Critical", issues.Count(i => i.Severity == "Critical"), XLColor.LightPink);
            WriteSummaryRow(sheet, r++, 1, "Error",    issues.Count(i => i.Severity == "Error"),    XLColor.LightSalmon);
            WriteSummaryRow(sheet, r++, 1, "Major",    issues.Count(i => i.Severity == "Major"),    XLColor.PeachPuff);
            WriteSummaryRow(sheet, r++, 1, "Warning",  issues.Count(i => i.Severity == "Warning"),  XLColor.LightYellow);
            WriteSummaryRow(sheet, r++, 1, "Minor",    issues.Count(i => i.Severity == "Minor"),    XLColor.LightGray);
            WriteSummaryRow(sheet, r++, 1, "Info",     issues.Count(i => i.Severity == "Info"),     XLColor.LightBlue);
            WriteSummaryRow(sheet, r++, 1, "TOTAL",    issues.Count,                                XLColor.LightSteelBlue, bold: true);

            r++;
            // Category breakdown
            WriteSummaryHeader(sheet, r++, 1, "Issues by Category");
            foreach (var g in issues.GroupBy(i => string.IsNullOrEmpty(i.Category) ? "General" : i.Category).OrderByDescending(g => g.Count()))
                WriteSummaryRow(sheet, r++, 1, g.Key, g.Count(), XLColor.LightCyan);

            r++;
            // Technical debt estimate
            WriteSummaryHeader(sheet, r++, 1, "Technical Debt");
            int debtMins = issues.Count * 8;
            WriteSummaryRow(sheet, r, 1, "Estimated Effort", debtMins < 60 ? $"{debtMins} min" :
                debtMins < 480 ? $"{debtMins / 60}h {debtMins % 60}min" :
                $"{debtMins / 480}d {(debtMins % 480) / 60}h", XLColor.LavenderBlue);

            sheet.Column(1).Width = 28;
            sheet.Column(2).Width = 20;
            sheet.Column(3).Width = 18;
        }

        private static void WriteSummaryHeader(IXLWorksheet sheet, int row, int col, string title)
        {
            sheet.Cell(row, col).Value = title;
            sheet.Cell(row, col).Style.Font.Bold = true;
            sheet.Cell(row, col).Style.Font.FontSize = 11;
            sheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.DarkSlateBlue;
            sheet.Cell(row, col).Style.Font.FontColor = XLColor.White;
            sheet.Range(row, col, row, col + 1).Merge();
        }

        private static void WriteSummaryRow(IXLWorksheet sheet, int row, int col, string label, object value, XLColor bg, bool bold = false)
        {
            sheet.Cell(row, col).Value = label;
            sheet.Cell(row, col + 1).Value = value?.ToString();
            sheet.Range(row, col, row, col + 1).Style.Fill.BackgroundColor = bg;
            if (bold)
            {
                sheet.Cell(row, col).Style.Font.Bold = true;
                sheet.Cell(row, col + 1).Style.Font.Bold = true;
            }
        }

        private void AddIssuesSheet(XLWorkbook workbook, List<CodeReviewIssue> issues)
        {
            var sheet = workbook.Worksheets.Add("Code Review");
            sheet.TabColor = XLColor.DarkRed;

            string[] headers =
            {
                "File", "Line", "Rule Id", "Category", "Severity",
                "Message", "Code (Actual)", "Suggested Fix", "Why It Matters", "Bad Code Example", "Good Code Fix", "Status"
            };

            for (int i = 0; i < headers.Length; i++)
                sheet.Cell(1, i + 1).Value = headers[i];

            var headerRange = sheet.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Font.FontColor = XLColor.White;
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
                sheet.Cell(row, 8).Value = issue.SuggestedFix;
                sheet.Cell(row, 9).Value = issue.Description;
                sheet.Cell(row, 10).Value = issue.BadCodeExample;
                sheet.Cell(row, 11).Value = issue.GoodCodeExample;
                sheet.Cell(row, 12).Value = "Open";

                switch (issue.Severity?.ToLower())
                {
                    case "critical": sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink; break;
                    case "error":    sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightSalmon; break;
                    case "warning":  sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow; break;
                }

                row++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.RangeUsed().SetAutoFilter();
            sheet.Columns().AdjustToContents();
        }

        private string GetCategory(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId))
                return "General";

            // Bug 16 fix: prefixes now match actual rule IDs in this project
            if (ruleId.StartsWith("SEC")) return "Security";
            if (ruleId.StartsWith("EFF")) return "Efficiency";
            if (ruleId.StartsWith("MAIN")) return "Maintainability";
            if (ruleId.StartsWith("DESIGN")) return "Design";
            if (ruleId.StartsWith("DES")) return "Design";
            if (ruleId.StartsWith("THR")) return "Threading";
            if (ruleId.StartsWith("STAB")) return "Stability";
            if (ruleId.StartsWith("RD")) return "Readability";
            if (ruleId.StartsWith("DOC")) return "Documentation";
            if (ruleId.StartsWith("EX") || ruleId.StartsWith("R0")) return "Exception Handling";
            if (ruleId.StartsWith("DB")) return "Database";
            if (ruleId.StartsWith("EF")) return "Entity Framework";

            return "General";
        }






    }
}



