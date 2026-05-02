using CodeReviewReporter.Models.CodeReviewReporter.Models;
using System.Text;
using System.Web;

namespace CodeReviewReporter.Services
{
    public class HtmlReportService
    {
        public void GenerateReport(List<CodeReviewIssue> issues, string outputPath, QualityGateSummary? gate = null)
        {
            var html = BuildHtml(issues, gate);
            File.WriteAllText(outputPath, html, Encoding.UTF8);
            Console.WriteLine($"HTML report generated: {Path.GetFullPath(outputPath)}");
        }

        private string BuildHtml(List<CodeReviewIssue> issues, QualityGateSummary? gate)
        {
            int critical = issues.Count(i => i.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase));
            int error = issues.Count(i => i.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
            int warning = issues.Count(i => i.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase));
            int info = issues.Count(i => i.Severity.Equals("Info", StringComparison.OrdinalIgnoreCase));
            int major = issues.Count(i => i.Severity.Equals("Major", StringComparison.OrdinalIgnoreCase));
            int minor = issues.Count(i => i.Severity.Equals("Minor", StringComparison.OrdinalIgnoreCase));

            var byCategory = issues
                .GroupBy(i => string.IsNullOrEmpty(i.Category) ? "General" : i.Category)
                .OrderByDescending(g => g.Count())
                .ToList();

            var topFiles = issues
                .GroupBy(i => Path.GetFileName(i.FileName ?? "Unknown"))
                .OrderByDescending(g => g.Count())
                .Take(8)
                .ToList();

            bool gatePassed = gate?.Passed ?? (critical == 0 && error <= 5);
            string gateColor = gatePassed ? "#28a745" : "#dc3545";
            string gateText = gatePassed ? "PASSED" : "FAILED";
            string gateIcon = gatePassed ? "✔" : "✘";

            int maxCat = byCategory.Any() ? byCategory.Max(g => g.Count()) : 1;

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
            sb.AppendLine("<title>Code Analysis Report</title>");
            sb.AppendLine(GetStyles());
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // ─── Header ───────────────────────────────────────────────────────
            sb.AppendLine($@"
<header>
  <div class=""header-inner"">
    <div class=""brand"">
      <span class=""brand-icon"">⬡</span>
      <div>
        <h1>CodeAnalyzer Pro</h1>
        <p>Static Analysis Report &nbsp;·&nbsp; {DateTime.Now:yyyy-MM-dd HH:mm}</p>
      </div>
    </div>
    <div class=""gate-badge"" style=""background:{gateColor}"">
      <span class=""gate-icon"">{gateIcon}</span>
      <div>
        <div class=""gate-label"">Quality Gate</div>
        <div class=""gate-value"">{gateText}</div>
      </div>
    </div>
  </div>
</header>");

            // ─── Metric Cards ──────────────────────────────────────────────────
            sb.AppendLine("<main>");
            sb.AppendLine("<section class=\"metrics\">");
            sb.AppendLine(MetricCard("Total Issues", issues.Count.ToString(), "#4a90e2", "⚠"));
            sb.AppendLine(MetricCard("Critical", critical.ToString(), "#dc3545", "🔴"));
            sb.AppendLine(MetricCard("Error", error.ToString(), "#e07b39", "🟠"));
            sb.AppendLine(MetricCard("Warning", warning.ToString(), "#f0ad4e", "🟡"));
            sb.AppendLine(MetricCard("Info", info.ToString(), "#5bc0de", "🔵"));
            sb.AppendLine(MetricCard("Tech Debt", FormatDebt(issues), "#6f42c1", "⏱"));
            sb.AppendLine("</section>");

            // ─── Quality Gate Failures ─────────────────────────────────────────
            if (gate != null && !gate.Passed && gate.FailureReasons.Any())
            {
                sb.AppendLine("<section class=\"card\">");
                sb.AppendLine("<h2 class=\"card-title\" style=\"color:#dc3545\">Quality Gate Failures</h2>");
                sb.AppendLine("<ul class=\"failure-list\">");
                foreach (var reason in gate.FailureReasons)
                    sb.AppendLine($"<li>{HttpUtility.HtmlEncode(reason)}</li>");
                sb.AppendLine("</ul></section>");
            }

            // ─── Charts Row ───────────────────────────────────────────────────
            sb.AppendLine("<div class=\"charts-row\">");

            // Category bar chart
            sb.AppendLine("<section class=\"card chart-card\">");
            sb.AppendLine("<h2 class=\"card-title\">Issues by Category</h2>");
            sb.AppendLine("<div class=\"bar-chart\">");
            foreach (var g in byCategory)
            {
                int pct = (int)((double)g.Count() / maxCat * 100);
                sb.AppendLine($@"
  <div class=""bar-row"">
    <span class=""bar-label"">{HttpUtility.HtmlEncode(g.Key)}</span>
    <div class=""bar-track""><div class=""bar-fill"" style=""width:{pct}%""></div></div>
    <span class=""bar-count"">{g.Count()}</span>
  </div>");
            }
            sb.AppendLine("</div></section>");

            // Severity donut (CSS-only)
            sb.AppendLine("<section class=\"card chart-card\">");
            sb.AppendLine("<h2 class=\"card-title\">Severity Breakdown</h2>");
            sb.AppendLine("<div class=\"severity-grid\">");
            AppendSeverityRow(sb, "Critical", critical, "#dc3545");
            AppendSeverityRow(sb, "Error", error, "#e07b39");
            AppendSeverityRow(sb, "Major", major, "#fd7e14");
            AppendSeverityRow(sb, "Warning", warning, "#f0ad4e");
            AppendSeverityRow(sb, "Minor", minor, "#a8b5c2");
            AppendSeverityRow(sb, "Info", info, "#5bc0de");
            sb.AppendLine("</div>");

            // Top files
            sb.AppendLine("<h2 class=\"card-title\" style=\"margin-top:1.5rem\">Top Files by Issues</h2>");
            sb.AppendLine("<div class=\"bar-chart\">");
            int maxFile = topFiles.Any() ? topFiles.Max(g => g.Count()) : 1;
            foreach (var g in topFiles)
            {
                int pct = (int)((double)g.Count() / maxFile * 100);
                sb.AppendLine($@"
  <div class=""bar-row"">
    <span class=""bar-label file-label"">{HttpUtility.HtmlEncode(g.Key)}</span>
    <div class=""bar-track""><div class=""bar-fill"" style=""width:{pct}%;background:#4a90e2""></div></div>
    <span class=""bar-count"">{g.Count()}</span>
  </div>");
            }
            sb.AppendLine("</div></section>");
            sb.AppendLine("</div>"); // charts-row

            // ─── Issues Table ──────────────────────────────────────────────────
            sb.AppendLine("<section class=\"card\">");
            sb.AppendLine("<div class=\"table-header\">");
            sb.AppendLine("<h2 class=\"card-title\">All Issues</h2>");
            sb.AppendLine(@"
<div class=""filters"">
  <input id=""search"" type=""text"" placeholder=""Search…"" oninput=""filterTable()"">
  <select id=""sevFilter"" onchange=""filterTable()"">
    <option value="""">All Severities</option>
    <option>Critical</option><option>Error</option><option>Major</option>
    <option>Warning</option><option>Minor</option><option>Info</option>
  </select>
  <select id=""catFilter"" onchange=""filterTable()"">
    <option value="""">All Categories</option>");

            foreach (var g in byCategory)
                sb.AppendLine($"    <option>{HttpUtility.HtmlEncode(g.Key)}</option>");

            sb.AppendLine("  </select>");
            sb.AppendLine("  <button onclick=\"exportCsv()\">Export CSV</button>");
            sb.AppendLine("</div></div>");

            sb.AppendLine("<div class=\"table-wrap\">");
            sb.AppendLine("<table id=\"issueTable\">");
            sb.AppendLine("<thead><tr><th>#</th><th>File</th><th>Line</th><th>Rule</th><th>Category</th><th>Severity</th><th>Message</th><th>Fix</th></tr></thead>");
            sb.AppendLine("<tbody>");

            int row = 1;
            foreach (var issue in issues)
            {
                string sev = issue.Severity ?? "Info";
                string sevClass = sev.ToLower() switch
                {
                    "critical" => "sev-critical",
                    "error" => "sev-error",
                    "major" => "sev-major",
                    "warning" => "sev-warning",
                    "minor" => "sev-minor",
                    _ => "sev-info"
                };
                string cat = HttpUtility.HtmlEncode(issue.Category ?? "General");
                string fix = HttpUtility.HtmlEncode(issue.SuggestedFix ?? "");

                sb.AppendLine($@"<tr data-sev=""{sev}"" data-cat=""{cat}"">
  <td>{row++}</td>
  <td title=""{HttpUtility.HtmlEncode(issue.FileName ?? "")}""><code>{HttpUtility.HtmlEncode(Path.GetFileName(issue.FileName ?? ""))}</code></td>
  <td>{issue.LineNumber}</td>
  <td><code class=""rule-id"">{HttpUtility.HtmlEncode(issue.RuleId ?? "")}</code></td>
  <td>{cat}</td>
  <td><span class=""sev-badge {sevClass}"">{sev}</span></td>
  <td>{HttpUtility.HtmlEncode(issue.Message ?? "")}</td>
  <td class=""fix-cell"">{fix}</td>
</tr>");
            }

            sb.AppendLine("</tbody></table></div></section>");
            sb.AppendLine("</main>");

            // ─── Footer ───────────────────────────────────────────────────────
            sb.AppendLine($@"
<footer>
  <p>Generated by <strong>CodeAnalyzer Pro</strong> · {DateTime.Now:yyyy-MM-dd HH:mm:ss} · {issues.Count} issues across {issues.Select(i => i.FileName).Distinct().Count()} files</p>
</footer>");

            sb.AppendLine(GetScripts());
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string MetricCard(string label, string value, string color, string icon) =>
            $@"<div class=""metric-card"" style=""border-top:4px solid {color}"">
  <div class=""metric-icon"">{icon}</div>
  <div class=""metric-value"" style=""color:{color}"">{value}</div>
  <div class=""metric-label"">{label}</div>
</div>";

        private static void AppendSeverityRow(StringBuilder sb, string sev, int count, string color)
        {
            sb.AppendLine($@"<div class=""sev-row"">
  <span class=""sev-dot"" style=""background:{color}""></span>
  <span class=""sev-name"">{sev}</span>
  <span class=""sev-cnt"" style=""color:{color}"">{count}</span>
</div>");
        }

        private static string FormatDebt(List<CodeReviewIssue> issues)
        {
            int mins = issues.Count * 8;
            if (mins < 60) return $"{mins}min";
            if (mins < 480) return $"{mins / 60}h {mins % 60}min";
            return $"{mins / 480}d {(mins % 480) / 60}h";
        }

        private static string GetStyles() => @"<style>
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:'Segoe UI',Arial,sans-serif;background:#f0f2f5;color:#333;font-size:14px}
header{background:linear-gradient(135deg,#1a237e 0%,#283593 100%);color:#fff;padding:1.2rem 2rem;box-shadow:0 2px 8px rgba(0,0,0,.3)}
.header-inner{display:flex;justify-content:space-between;align-items:center;max-width:1600px;margin:0 auto}
.brand{display:flex;align-items:center;gap:1rem}
.brand-icon{font-size:2.5rem}
.brand h1{font-size:1.6rem;font-weight:700}
.brand p{opacity:.75;font-size:.85rem;margin-top:.2rem}
.gate-badge{display:flex;align-items:center;gap:.8rem;padding:.8rem 1.4rem;border-radius:10px;color:#fff;min-width:160px}
.gate-icon{font-size:2rem}
.gate-label{font-size:.75rem;opacity:.85;text-transform:uppercase;letter-spacing:.5px}
.gate-value{font-size:1.4rem;font-weight:700}
main{max-width:1600px;margin:1.5rem auto;padding:0 1.5rem}
.metrics{display:grid;grid-template-columns:repeat(auto-fill,minmax(170px,1fr));gap:1rem;margin-bottom:1.5rem}
.metric-card{background:#fff;border-radius:10px;padding:1.2rem;text-align:center;box-shadow:0 2px 6px rgba(0,0,0,.08);transition:transform .15s}
.metric-card:hover{transform:translateY(-3px)}
.metric-icon{font-size:1.5rem;margin-bottom:.4rem}
.metric-value{font-size:2rem;font-weight:700}
.metric-label{font-size:.8rem;color:#888;margin-top:.3rem;text-transform:uppercase;letter-spacing:.5px}
.charts-row{display:grid;grid-template-columns:1fr 1fr;gap:1rem;margin-bottom:1.5rem}
@media(max-width:900px){.charts-row{grid-template-columns:1fr}}
.card{background:#fff;border-radius:10px;padding:1.5rem;box-shadow:0 2px 6px rgba(0,0,0,.08);margin-bottom:1.5rem}
.card-title{font-size:1rem;font-weight:600;color:#1a237e;margin-bottom:1rem;border-bottom:2px solid #e8eaf6;padding-bottom:.5rem}
.chart-card{overflow:hidden}
.bar-chart{display:flex;flex-direction:column;gap:.6rem}
.bar-row{display:grid;grid-template-columns:140px 1fr 50px;align-items:center;gap:.6rem}
.bar-label{font-size:.82rem;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;color:#555}
.file-label{font-size:.75rem}
.bar-track{background:#eef0f8;border-radius:4px;height:18px;overflow:hidden}
.bar-fill{height:100%;background:linear-gradient(90deg,#3f51b5,#7986cb);border-radius:4px;transition:width .4s}
.bar-count{font-size:.82rem;font-weight:600;color:#3f51b5;text-align:right}
.severity-grid{display:flex;flex-direction:column;gap:.7rem}
.sev-row{display:flex;align-items:center;gap:.7rem}
.sev-dot{width:12px;height:12px;border-radius:50%;flex-shrink:0}
.sev-name{flex:1;font-size:.85rem}
.sev-cnt{font-weight:700;font-size:1rem;min-width:30px;text-align:right}
.table-header{display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:.8rem;margin-bottom:1rem}
.filters{display:flex;gap:.6rem;flex-wrap:wrap;align-items:center}
.filters input,.filters select{padding:.4rem .7rem;border:1px solid #ddd;border-radius:6px;font-size:.83rem;background:#fff}
.filters button{padding:.4rem .9rem;background:#3f51b5;color:#fff;border:none;border-radius:6px;cursor:pointer;font-size:.83rem;transition:background .15s}
.filters button:hover{background:#303f9f}
.table-wrap{overflow-x:auto}
table{width:100%;border-collapse:collapse;font-size:.82rem}
thead th{background:#1a237e;color:#fff;padding:.6rem .8rem;text-align:left;white-space:nowrap;position:sticky;top:0}
tbody tr:nth-child(even){background:#f8f9ff}
tbody tr:hover{background:#e8eaf6}
td{padding:.5rem .8rem;border-bottom:1px solid #e9ecef;vertical-align:top}
code{font-family:'Cascadia Code','Consolas',monospace;font-size:.8rem}
.rule-id{background:#e8eaf6;padding:.1rem .4rem;border-radius:4px;color:#3f51b5}
.sev-badge{display:inline-block;padding:.15rem .55rem;border-radius:20px;font-size:.75rem;font-weight:600;color:#fff;white-space:nowrap}
.sev-critical{background:#dc3545}
.sev-error{background:#e07b39}
.sev-major{background:#fd7e14}
.sev-warning{background:#f0ad4e;color:#333}
.sev-minor{background:#a8b5c2;color:#333}
.sev-info{background:#5bc0de}
.fix-cell{max-width:220px;font-size:.78rem;color:#555;word-break:break-word}
.failure-list{padding-left:1.2rem;color:#dc3545;line-height:1.8}
footer{text-align:center;padding:1.5rem;color:#888;font-size:.8rem;border-top:1px solid #e0e0e0;margin-top:1rem}
</style>";

        private static string GetScripts() => @"<script>
function filterTable(){
  const search=document.getElementById('search').value.toLowerCase();
  const sev=document.getElementById('sevFilter').value.toLowerCase();
  const cat=document.getElementById('catFilter').value.toLowerCase();
  document.querySelectorAll('#issueTable tbody tr').forEach(r=>{
    const txt=r.textContent.toLowerCase();
    const rs=r.dataset.sev?r.dataset.sev.toLowerCase():'';
    const rc=r.dataset.cat?r.dataset.cat.toLowerCase():'';
    const show=(search===''||txt.includes(search))&&
               (sev===''||rs===sev)&&
               (cat===''||rc.includes(cat.toLowerCase()));
    r.style.display=show?'':'none';
  });
}
function exportCsv(){
  const rows=[['#','File','Line','Rule','Category','Severity','Message','Fix']];
  document.querySelectorAll('#issueTable tbody tr').forEach(r=>{
    if(r.style.display==='none')return;
    const cells=[...r.querySelectorAll('td')].map(c=>'""+c.textContent.replace(/[""\n]/g,' ')+'"");
    rows.push(cells);
  });
  const csv=rows.map(r=>r.join(',')).join('\n');
  const a=document.createElement('a');
  a.href='data:text/csv;charset=utf-8,'+encodeURIComponent(csv);
  a.download='code-analysis-report.csv';
  a.click();
}
</script>";
    }

    public class QualityGateSummary
    {
        public bool Passed { get; set; }
        public List<string> FailureReasons { get; set; } = new();
    }
}
