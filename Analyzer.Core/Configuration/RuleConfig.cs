using Analyzer.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Configuration
{
    public class RuleConfig
    {
        public Dictionary<string, RuleSettings> Rules { get; set; } = new();
    }

    public class RuleSettings
    {
        public bool Enabled { get; set; } = true;

        // Optional override
        public Severity? Severity { get; set; }

        // Flexible thresholds
        public Dictionary<string, int> Parameters { get; set; } = new();
    }

}
