using System.ComponentModel.DataAnnotations.Schema;

namespace Analyzer1.API.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? LastScan { get; set; }
        public string QualityGateStatus { get; set; } = "Unknown";
        [NotMapped]
        public int IssueCount { get; set; }
    }
}
