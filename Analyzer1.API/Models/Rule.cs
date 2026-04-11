namespace Analyzer1.API.Models
{
    public class Rule
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Severity { get; set; }
        public bool Enabled { get; set; }
    }
}
