using System.Text;
using System.Text.Json;
using System.Web;
using Analyzer.Core.Interface;
using Analyzer.Core.Models;

namespace Analyzer1.Runner
{
    public class HtmlReportGenerator : IReportGenerator
    {
        public async Task GenerateAsync(IReadOnlyList<CodeIssue> issues, string outputPath)
        {
            Directory.CreateDirectory(outputPath);

            var jsonPath = Path.Combine(outputPath, "data.json");
            var htmlPath = Path.Combine(outputPath, "index.html");

            var json = JsonSerializer.Serialize(issues, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(jsonPath, json);

            var html = BuildHtml(issues);
            await File.WriteAllTextAsync(htmlPath, html);
        }

        private static string BuildHtml(IReadOnlyList<CodeIssue> issues)
        {
            int critical = issues.Count(i => i.Severity == Analyzer.Core.Enums.Severity.Critical);
            int error    = issues.Count(i => i.Severity == Analyzer.Core.Enums.Severity.Error);
            int warning  = issues.Count(i => i.Severity == Analyzer.Core.Enums.Severity.Warning);
            int info     = issues.Count(i => i.Severity == Analyzer.Core.Enums.Severity.Info);

            bool gatePassed = critical == 0 && error <= 5;
            string gateColor = gatePassed ? "#28a745" : "#dc3545";
            string gateText  = gatePassed ? "PASSED" : "FAILED";

            var byCategory = issues
                .GroupBy(i => string.IsNullOrEmpty(i.Category) ? "General" : i.Category)
                .OrderByDescending(g => g.Count()).ToList();
            int maxCat = byCategory.Any() ? byCategory.Max(g => g.Count()) : 1;

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\">");
            sb.AppendLine("<title>Code Analysis Report</title>");
            sb.AppendLine(Styles());
            sb.AppendLine("</head><body>");

            sb.AppendLine($@"
<header>
  <div class=""hi"">
    <div class=""brand""><span style=""font-size:2rem"">⬡</span>
      <div><h1>CodeAnalyzer Pro</h1><p>{DateTime.Now:yyyy-MM-dd HH:mm}</p></div>
    </div>
    <div class=""gate"" style=""background:{gateColor}"">
      <div class=""gl"">Quality Gate</div>
      <div class=""gv"">{gateText}</div>
    </div>
  </div>
</header><main>");

            // metric cards
            sb.AppendLine("<section class=\"mc\">");
            sb.AppendLine(Card("Total",    issues.Count.ToString(), "#4a90e2", "⚠"));
            sb.AppendLine(Card("Critical", critical.ToString(),     "#dc3545", "🔴"));
            sb.AppendLine(Card("Error",    error.ToString(),        "#e07b39", "🟠"));
            sb.AppendLine(Card("Warning",  warning.ToString(),      "#f0ad4e", "🟡"));
            sb.AppendLine(Card("Info",     info.ToString(),         "#5bc0de", "🔵"));
            sb.AppendLine("</section>");

            // category chart
            sb.AppendLine("<section class=\"card\"><h2>Issues by Category</h2><div class=\"bc\">");
            foreach (var g in byCategory)
            {
                int pct = (int)((double)g.Count() / maxCat * 100);
                sb.AppendLine($@"<div class=""br""><span class=""bl"">{HttpUtility.HtmlEncode(g.Key)}</span>
<div class=""bt""><div class=""bf"" style=""width:{pct}%""></div></div>
<span class=""bn"">{g.Count()}</span></div>");
            }
            sb.AppendLine("</div></section>");

            // issues table
            sb.AppendLine(@"<section class=""card"">
<div class=""th""><h2>All Issues <small style=""font-weight:400;color:#888"">(click any row to expand fix details)</small></h2>
<div class=""fi"">
<input id=""s"" type=""text"" placeholder=""Search…"" oninput=""f()"">
<select id=""sv"" onchange=""f()""><option value="""">All Severities</option>
<option>Critical</option><option>Error</option><option>Warning</option><option>Info</option></select>
<button onclick=""csv()"">Export CSV</button></div></div>
<div style=""overflow-x:auto""><table id=""t"">
<thead><tr><th>▶</th><th>#</th><th>File</th><th>Line</th><th>Rule</th><th>Category</th><th>Severity</th><th>Message</th><th>Suggested Fix</th></tr></thead><tbody>");

            int row = 1;
            foreach (var issue in issues)
            {
                string sev  = issue.Severity.ToString();
                string sc   = sev.ToLower() switch
                {
                    "critical" => "sc", "error" => "se", "warning" => "sw", _ => "si"
                };
                string rowId = $"d{row}";
                string suggestedFix = string.IsNullOrEmpty(issue.SuggestedFix) ? issue.Remediation : issue.SuggestedFix;

                sb.AppendLine($@"<tr data-s=""{sev}"" class=""irow"" onclick=""tgl('{rowId}')"">
<td class=""ex"">▶</td>
<td>{row}</td>
<td><code>{HttpUtility.HtmlEncode(Path.GetFileName(issue.FilePath))}</code></td>
<td>{issue.Line}</td>
<td><code class=""rid"">{HttpUtility.HtmlEncode(issue.RuleId)}</code></td>
<td>{HttpUtility.HtmlEncode(issue.Category)}</td>
<td><span class=""sb {sc}"">{sev}</span></td>
<td>{HttpUtility.HtmlEncode(issue.Message)}</td>
<td class=""fix"">{HttpUtility.HtmlEncode(suggestedFix)}</td></tr>");

                // Detail expansion row
                var whyHtml = string.IsNullOrEmpty(issue.WhyItMatters)
                    ? ""
                    : $"<div class=\"dp-why\"><span class=\"dp-lbl\">Why it matters:</span> {HttpUtility.HtmlEncode(issue.WhyItMatters)}</div>";

                var badHtml = string.IsNullOrEmpty(issue.BadCodeExample)
                    ? ""
                    : $"<div class=\"dp-code bad\"><div class=\"dp-lbl bad-lbl\">Bad Code (Problem)</div><pre><code>{HttpUtility.HtmlEncode(issue.BadCodeExample)}</code></pre></div>";

                var goodHtml = string.IsNullOrEmpty(issue.GoodCodeExample)
                    ? ""
                    : $"<div class=\"dp-code good\"><div class=\"dp-lbl good-lbl\">Good Code (Fix)</div><pre><code>{HttpUtility.HtmlEncode(issue.GoodCodeExample)}</code></pre></div>";

                var fixHtml = string.IsNullOrEmpty(suggestedFix)
                    ? ""
                    : $"<div class=\"dp-fix\"><span class=\"dp-lbl\">Suggested Fix:</span> {HttpUtility.HtmlEncode(suggestedFix)}</div>";

                sb.AppendLine($@"<tr id=""{rowId}"" class=""drow"" style=""display:none"" data-s=""{sev}"">
<td colspan=""9"">
<div class=""dp"">
{whyHtml}
<div class=""dp-examples"">{badHtml}{goodHtml}</div>
{fixHtml}
</div>
</td></tr>");

                row++;
            }

            sb.AppendLine("</tbody></table></div></section></main>");
            sb.AppendLine($"<footer>CodeAnalyzer Pro · {issues.Count} issues · {DateTime.Now:yyyy-MM-dd}</footer>");
            sb.AppendLine(Scripts());
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string Card(string label, string value, string color, string icon) =>
            $@"<div class=""mc-card"" style=""border-top:4px solid {color}"">
<div style=""font-size:1.4rem"">{icon}</div>
<div class=""mv"" style=""color:{color}"">{value}</div>
<div class=""ml"">{label}</div></div>";

        private static string Styles() => @"<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:'Segoe UI',Arial,sans-serif;background:#f0f2f5;color:#333;font-size:14px}
header{background:linear-gradient(135deg,#1a237e,#283593);color:#fff;padding:1rem 2rem}
.hi{display:flex;justify-content:space-between;align-items:center;max-width:1400px;margin:0 auto}
.brand{display:flex;align-items:center;gap:1rem} .brand h1{font-size:1.5rem} .brand p{opacity:.7;font-size:.8rem}
.gate{display:flex;flex-direction:column;align-items:center;padding:.8rem 1.6rem;border-radius:10px}
.gl{font-size:.7rem;opacity:.85;text-transform:uppercase} .gv{font-size:1.4rem;font-weight:700}
main{max-width:1400px;margin:1.5rem auto;padding:0 1.5rem}
.mc{display:grid;grid-template-columns:repeat(auto-fill,minmax(160px,1fr));gap:1rem;margin-bottom:1.5rem}
.mc-card{background:#fff;border-radius:10px;padding:1.2rem;text-align:center;box-shadow:0 2px 6px rgba(0,0,0,.08)}
.mv{font-size:2rem;font-weight:700} .ml{font-size:.75rem;color:#888;margin-top:.3rem;text-transform:uppercase}
.card{background:#fff;border-radius:10px;padding:1.4rem;box-shadow:0 2px 6px rgba(0,0,0,.08);margin-bottom:1.5rem}
.card h2{font-size:1rem;font-weight:600;color:#1a237e;margin-bottom:.8rem;border-bottom:2px solid #e8eaf6;padding-bottom:.4rem}
.bc{display:flex;flex-direction:column;gap:.5rem}
.br{display:grid;grid-template-columns:140px 1fr 45px;align-items:center;gap:.5rem}
.bl{font-size:.82rem;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:#555}
.bt{background:#eef0f8;border-radius:4px;height:16px;overflow:hidden}
.bf{height:100%;background:linear-gradient(90deg,#3f51b5,#7986cb);border-radius:4px}
.bn{font-size:.82rem;font-weight:600;color:#3f51b5;text-align:right}
.th{display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:.6rem;margin-bottom:.8rem}
.fi{display:flex;gap:.5rem;flex-wrap:wrap}
.fi input,.fi select{padding:.35rem .6rem;border:1px solid #ddd;border-radius:6px;font-size:.82rem}
.fi button{padding:.35rem .8rem;background:#3f51b5;color:#fff;border:none;border-radius:6px;cursor:pointer}
table{width:100%;border-collapse:collapse;font-size:.82rem}
thead th{background:#1a237e;color:#fff;padding:.5rem .7rem;text-align:left;position:sticky;top:0}
tbody tr.irow:hover{background:#e8eaf6;cursor:pointer}
tbody tr.irow:nth-child(4n+1){background:#f8f9ff}
td{padding:.45rem .7rem;border-bottom:1px solid #e9ecef;vertical-align:top}
code{font-family:'Consolas',monospace;font-size:.78rem}
.rid{background:#e8eaf6;padding:.1rem .35rem;border-radius:4px;color:#3f51b5}
.sb{display:inline-block;padding:.12rem .5rem;border-radius:20px;font-size:.73rem;font-weight:600;color:#fff}
.sc{background:#dc3545}.se{background:#e07b39}.sw{background:#f0ad4e;color:#333}.si{background:#5bc0de}
.fix{max-width:260px;font-size:.75rem;color:#2d6a2d;word-break:break-word;font-style:italic}
.ex{color:#3f51b5;font-size:.9rem;user-select:none;min-width:20px}
/* Detail panel */
.drow td{padding:0;background:#f7f9ff;border-bottom:2px solid #c5cae9}
.dp{padding:1rem 1.5rem;display:grid;grid-template-columns:1fr;gap:.75rem}
.dp-why{background:#fff8e1;border-left:4px solid #f0ad4e;padding:.6rem .9rem;border-radius:0 6px 6px 0;font-size:.82rem;color:#555}
.dp-lbl{font-weight:700;color:#1a237e;display:block;margin-bottom:.3rem;font-size:.75rem;text-transform:uppercase;letter-spacing:.04em}
.dp-examples{display:grid;grid-template-columns:1fr 1fr;gap:.75rem}
@media(max-width:900px){.dp-examples{grid-template-columns:1fr}}
.dp-code{border-radius:6px;overflow:hidden;font-size:.78rem}
.dp-code pre{padding:.7rem 1rem;overflow-x:auto;margin:0}
.dp-code code{font-family:'Consolas',monospace;white-space:pre;font-size:.78rem}
.bad{border:1px solid #f5c6cb} .bad pre{background:#fff5f5}
.bad-lbl{background:#dc3545;color:#fff;padding:.3rem .75rem}
.good{border:1px solid #c3e6cb} .good pre{background:#f5fff8}
.good-lbl{background:#28a745;color:#fff;padding:.3rem .75rem}
.dp-fix{background:#e8f4fd;border-left:4px solid #3f51b5;padding:.6rem .9rem;border-radius:0 6px 6px 0;font-size:.82rem;color:#1a237e;font-weight:500}
footer{text-align:center;padding:1rem;color:#888;font-size:.78rem;border-top:1px solid #e0e0e0}
</style>";

        private static string Scripts() => @"<script>
function tgl(id){
  var d=document.getElementById(id);
  var irow=d.previousElementSibling;
  var ex=irow?irow.querySelector('.ex'):null;
  if(d.style.display==='none'){
    d.style.display='';
    if(ex)ex.textContent='▼';
  } else {
    d.style.display='none';
    if(ex)ex.textContent='▶';
  }
}
function f(){
  var s=document.getElementById('s').value.toLowerCase();
  var sv=document.getElementById('sv').value.toLowerCase();
  var rows=[...document.querySelectorAll('#t tbody tr.irow')];
  rows.forEach(function(r){
    var txt=r.textContent.toLowerCase();
    var rs=r.dataset.s?r.dataset.s.toLowerCase():'';
    var show=(s===''||txt.includes(s))&&(sv===''||rs===sv);
    r.style.display=show?'':'none';
    var detail=r.nextElementSibling;
    if(detail&&detail.classList.contains('drow'))
      detail.style.display=show&&detail.style.display!=='none'?'':'none';
  });
}
function csv(){
  var rows=[['#','File','Line','Rule','Category','Severity','Message','Suggested Fix']];
  document.querySelectorAll('#t tbody tr.irow').forEach(function(r){
    if(r.style.display==='none')return;
    var cells=[...r.querySelectorAll('td')].slice(1);
    rows.push(cells.map(function(c){return'""'+c.textContent.trim().replace(/[""\n\r]/g,' ')+'""';}));
  });
  var a=document.createElement('a');
  a.href='data:text/csv;charset=utf-8,﻿'+encodeURIComponent(rows.map(function(r){return r.join(',');}).join('\n'));
  a.download='analysis.csv';a.click();
}
</script>";
    }
}
