using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Models
{
    public class CallSiteInfo
    {
        public string FilePath { get; set; }
        public string ClassName { get; set; }
        public int Line { get; set; }
    }
}
