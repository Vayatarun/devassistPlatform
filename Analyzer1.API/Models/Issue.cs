namespace Analyzer1.API.Models
{
    public class Issue
    {
        public int Id { get; set; }
        public string Rule { get; set; }
        public string Severity { get; set; }
        public string FilePath { get; set; }
        public int Line { get; set; }
        public string Message { get; set; }
        public int ProjectId { get; set; }
    }
}
