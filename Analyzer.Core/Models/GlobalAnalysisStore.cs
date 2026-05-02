using System.Collections.Concurrent;

namespace Analyzer.Core.Models
{
    public class GlobalAnalysisStore
    {
        public ConcurrentDictionary<string, ConcurrentBag<CodeBlockInfo>> DuplicateMap { get; }
            = new ConcurrentDictionary<string, ConcurrentBag<CodeBlockInfo>>();

        public ConcurrentDictionary<string, ConcurrentBag<CallSiteInfo>> MethodUsageMap { get; }
            = new ConcurrentDictionary<string, ConcurrentBag<CallSiteInfo>>();
    }
}
