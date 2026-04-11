using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Analyzer.Core.Models;
using System.Text.Json;
using Analyzer.Core.Interface;
namespace Analyzer1.Runner
{
 

    public class HtmlReportGenerator : IReportGenerator
    {
        public async Task GenerateAsync(
            IReadOnlyList<CodeIssue> issues,
            string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            var jsonPath = Path.Combine(outputPath, "data.json");
            var htmlPath = Path.Combine(outputPath, "index.html");

            var json = JsonSerializer.Serialize(
                issues,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await File.WriteAllTextAsync(jsonPath, json);

            var html = GenerateHtmlTemplate();

            await File.WriteAllTextAsync(htmlPath, html);
        }

        private string GenerateHtmlTemplate()
        {
            return """
<!DOCTYPE html>
<html>
<head>
    <title>Analyzer Dashboard</title>
    <script>
        async function loadData() {
            const response = await fetch('data.json');
            const issues = await response.json();

            const table = document.getElementById('issues');

            issues.forEach(issue => {
                const row = table.insertRow();
                row.insertCell(0).innerText = issue.filePath;
                row.insertCell(1).innerText = issue.line;
                row.insertCell(2).innerText = issue.message;
                row.insertCell(3).innerText = issue.ruleId;
            });
        }
    </script>
</head>
<body onload="loadData()">
    <h1>Code Analysis Report</h1>
    <table border="1">
        <thead>
            <tr>
                <th>File</th>
                <th>Line</th>
                <th>Message</th>
                <th>Rule</th>
            </tr>
        </thead>
        <tbody id="issues"></tbody>
    </table>
</body>
</html>
""";
        }
    }
}
