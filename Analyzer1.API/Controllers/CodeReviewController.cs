using CodeReviewReporter.Models.CodeReviewReporter.Models;
using CodeReviewReporter.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Analyzer1.API.Controllers
{
    [ApiController]
    [Route("api/review")]
    public class CodeReviewController : ControllerBase
    {
        private readonly SvnCommitAnalyzer _diffAnalyzer = new();
        private readonly RoslynAnalyzerService _roslyn = new();
        private readonly MultiLanguageAnalyzerService multiLanguageAnalyzerService=new();
        private readonly ExcelReportService _excel = new();
        private readonly EnterpriseCodeReviewExcel _enterpriseCodeReviewExcel=new();

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDiff([FromForm] IFormFile diffFile, [FromForm] string Sheettype = "0")
        {
            if (diffFile == null || diffFile.Length == 0)
                return BadRequest("No file uploaded");

            string diffContent;

            using (var reader = new StreamReader(diffFile.OpenReadStream()))
            {
                diffContent = await reader.ReadToEndAsync();
            }

            var files = _diffAnalyzer.ExtractCodeFromDiff(diffContent);

            var issues = new List<CodeReviewIssue>();

            foreach (var file in files)
            {
                var result = await multiLanguageAnalyzerService.AnalyzeCode(file.file, file.code);
                //var result = await _roslyn.AnalyzeCode(file.file, file.code);
                issues.AddRange(result);
            }

            var stream = new MemoryStream();
            if (Sheettype == "0")
            {
                _excel.GenerateIssueReportToStream(issues, stream);

                stream.Position = 0;
            }
            else
            {
                _enterpriseCodeReviewExcel.GenerateFullChecklistToStream(stream);
                stream.Position = 0;
            }
            return File(
                stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "CodeReviewReport.xlsx");
        }

    }
}
