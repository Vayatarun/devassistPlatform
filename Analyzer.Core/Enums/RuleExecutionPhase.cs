using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Enums
{
    public enum RuleExecutionPhase
    {
        Collector,   // Pass 1
        Analyzer     // Pass 2
    }
}
