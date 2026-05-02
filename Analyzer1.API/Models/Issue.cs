namespace Analyzer1.API.Models
{
    public class Issue
    {
        public int Id { get; set; }
        public string Rule { get; set; } = string.Empty;
        public string RuleTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info";
        public string FilePath { get; set; } = string.Empty;
        public int Line { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Remediation { get; set; }
        public string? SuggestedFix { get; set; }
        public string? WhyItMatters { get; set; }
        public string? BadCodeExample { get; set; }
        public string? GoodCodeExample { get; set; }
        public int EffortMinutes { get; set; }
        public string Status { get; set; } = "Open";
        public int ProjectId { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.Now;
    }
}
