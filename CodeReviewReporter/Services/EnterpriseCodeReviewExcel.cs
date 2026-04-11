using ClosedXML.Excel;
using CodeReviewReporter.Models.CodeReviewReporter.Models;
using System;
using System.Collections.Generic;


namespace CodeReviewReporter.Services
{
    public class CodeReviewItem
    {
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string Owner { get; set; }
    }

    public class EnterpriseCodeReviewExcel
    {
        public void GenerateFullChecklist(string outputPath)
        {
            var items = GetFullChecklist();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Code Review");

            // Headers
            var headers = new[] { "Structure", "Code Readability", "Description", "Severity", "Status", "Remarks", "Owner" };
            for (int c = 0; c < headers.Length; c++)
            {
                sheet.Cell(1, c + 1).Value = headers[c];
                sheet.Cell(1, c + 1).Style.Font.Bold = true;
                sheet.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Fill data
            int row = 2;
            foreach (var item in items)
            {
                sheet.Cell(row, 1).Value = item.Category;
                sheet.Cell(row, 2).Value = item.SubCategory;
                sheet.Cell(row, 3).Value = item.Description;
                sheet.Cell(row, 4).Value = item.Severity;
                sheet.Cell(row, 5).Value = item.Status;
                sheet.Cell(row, 6).Value = item.Remarks;
                sheet.Cell(row, 7).Value = item.Owner;

                // Highlight severity
                if (item.Severity.Equals("High", StringComparison.OrdinalIgnoreCase))
                    sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
                else if (item.Severity.Equals("Medium", StringComparison.OrdinalIgnoreCase))
                    sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;
                else if (item.Severity.Equals("Low", StringComparison.OrdinalIgnoreCase))
                    sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightGreen;

                row++;
            }

            // Auto adjust columns
            sheet.Columns().AdjustToContents();

            workbook.SaveAs(outputPath);
            Console.WriteLine($"Enterprise Code Review Excel generated at: {outputPath}");
        }

        private List<CodeReviewItem> GetFullChecklist()
        {
            return new List<CodeReviewItem>
        {
            new CodeReviewItem { Category="Structure", SubCategory="Code Readability",
                Description="Is the code well-structured, consistent in style, and consistently formatted? Eg. Indentation, Alignment, Use of Camelcase",
                Severity="Low", Status="FC", Remarks="Method length is very large (~180+ lines) Can be rewrite using Multiple Functions.", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Code Commenting",
                Description="Any change in existing code, should be mentioned on page, Why / When / by Whom / What",
                Severity="Medium", Status="FC", Remarks="Partially Implemented", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Exception handling",
                Description="Proper implementation of Exception Handling (try/catch and finally blocks) and logging of exceptions.",
                Severity="High", Status="PC", Remarks="remove ex as it removes original stack trace.", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Unused Code/Variable Objects",
                Description="Remove unused/commented codes/declared and assigned variables/object if not being used for a long time in code.",
                Severity="Medium", Status="PC", Remarks="bool isvalid = true; remove and use directly return true or return false", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Boundary Conditions",
                Description="What is going into and out of a method is what you expect e.g. no nulls",
                Severity="High", Status="NC", Remarks="accommodationHeadIds.Contains(e.HEAD_ID.Value) check e.HEAD_ID.HasValue for null exception", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="SQL Injection Checks",
                Description="This must be handled on Login Page",
                Severity="High", Status="FC", Remarks="", Owner="FC" },

            new CodeReviewItem { Category="Data Access Code", SubCategory="Exception handling",
                Description="Use of already existing common classes for database related operations.",
                Severity="High", Status="PC", Remarks="accommodationHeadIds.Contains(e.HEAD_ID.Value) check e.HEAD_ID.HasValue for null exception", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Use of Transactions",
                Description="Use transaction wherever required",
                Severity="High", Status="PC", Remarks="DbContext not disposed var context = DataContext", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Use of Binary Large Objects (BLOBS)",
                Description="Use BLOB instead of file system", Severity="Low", Status="FC", Remarks="", Owner="FC" },

            new CodeReviewItem { Category="Common Performance Issues", SubCategory="String management",
                Description="Use StringBuilder for complex string manipulations and when you need to concatenate strings multiple times",
                Severity="High", Status="FC", Remarks="", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Presentation Layer",
                Description="Do not call any function from HTML View if the function is written for retrieving data from database.",
                Severity="High", Status="FC", Remarks="", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Database",
                Description="Do not load data from database unnecessarily until it is required to load.",
                Severity="Medium", Status="FC", Remarks="", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Entity Framework",
                Description="To delete a record permanently from database table using Entity Framework, do not load that record in memory.",
                Severity="Low", Status="FC", Remarks="", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Project Warnings",
                Description="Make sure that there shouldn't be any project warnings. If you see any project warning in Error List window, please remove them immediately at any cost.",
                Severity="Medium", Status="FC", Remarks="N/A", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Unnecessary Imports",
                Description="All unused usings need to be removed. Code cleanup for unnecessary code is always a good practice.",
                Severity="High", Status="FC", Remarks="N/A", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Type Casting",
                Description="Avoid type casting and type conversions as much as possible; because it is a performance penalty.",
                Severity="Medium", Status="FC", Remarks="", Owner="FC" },

            new CodeReviewItem { Category="Arithmetic Operation", SubCategory="Arithmetic Operator",
                Description="Are divisors tested for zero.", Severity="High", Status="FC", Remarks="", Owner="FC" },

            new CodeReviewItem { Category="Logical Operations", SubCategory="Loops & Branches",
                Description="Are loop termination conditions obvious and invariably achievable?", Severity="High", Status="FC", Remarks="", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Loops & Branches",
                Description="Are indexes or subscripts properly initialized, just prior to the loop?", Severity="High", Status="FC", Remarks="", Owner="FC" },

            new CodeReviewItem { Category="", SubCategory="Unreachable Code",
                Description="Check whether any unreachable code exists and modify the code if it exists.", Severity="High", Status="FC", Remarks="", Owner="FC" },
        };
        }



        public void GenerateFullChecklistToStream( Stream stream)
        {
            var items = GetFullChecklist();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Code Review");

            // Headers
            var headers = new[] { "Structure", "Code Readability", "Description", "Severity", "Status", "Remarks", "Owner" };
            for (int c = 0; c < headers.Length; c++)
            {
                sheet.Cell(1, c + 1).Value = headers[c];
                sheet.Cell(1, c + 1).Style.Font.Bold = true;
                sheet.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Fill data
            int row = 2;
            foreach (var item in items)
            {
                sheet.Cell(row, 1).Value = item.Category;
                sheet.Cell(row, 2).Value = item.SubCategory;
                sheet.Cell(row, 3).Value = item.Description;
                sheet.Cell(row, 4).Value = item.Severity;
                sheet.Cell(row, 5).Value = item.Status;
                sheet.Cell(row, 6).Value = item.Remarks;
                sheet.Cell(row, 7).Value = item.Owner;

                // Highlight severity
                if (item.Severity.Equals("High", StringComparison.OrdinalIgnoreCase))
                    sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
                else if (item.Severity.Equals("Medium", StringComparison.OrdinalIgnoreCase))
                    sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;
                else if (item.Severity.Equals("Low", StringComparison.OrdinalIgnoreCase))
                    sheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightGreen;

                row++;
            }

            // Auto adjust columns
            sheet.Columns().AdjustToContents();

            workbook.SaveAs(stream);
        }


    }

}