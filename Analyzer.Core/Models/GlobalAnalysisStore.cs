using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Models
{
    public class GlobalAnalysisStore
    {
        // Hash → list of occurrences across files
        public Dictionary<string, List<CodeBlockInfo>> DuplicateMap { get; }
            = new Dictionary<string, List<CodeBlockInfo>>();
      
            public Dictionary<string, List<CallSiteInfo>> MethodUsageMap { get; } = new Dictionary<string, List<CallSiteInfo>>();

    }
}
