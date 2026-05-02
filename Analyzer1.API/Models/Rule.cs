namespace Analyzer1.API.Models
{
    public class Rule
    {
        public int Id { get; set; }
        public string RuleId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = "Warning";
        public bool Enabled { get; set; } = true;
        public string? Description { get; set; }
        public string? Remediation { get; set; }
        public string? Tags { get; set; }
    }
}
