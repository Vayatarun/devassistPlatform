namespace Analyzer1.API.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime LastScan { get; set; }
    }
}
