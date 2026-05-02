namespace Analyzer1.API.Models
{
    public class AnalysisScan
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = "Running";
        public int TotalIssues { get; set; }
        public int CriticalCount { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int TechnicalDebtMinutes { get; set; }
        public bool QualityGatePassed { get; set; }
    }
}
