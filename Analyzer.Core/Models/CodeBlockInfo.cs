using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Models
{
    public class CodeBlockInfo
    {
        public string FilePath { get; set; }
        public int Line { get; set; }
        public string MethodName { get; set; }
    }
}
