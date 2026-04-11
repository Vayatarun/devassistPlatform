using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeReviewReporter.Models
{
    public enum ChangeType
    {
        Added,
        Modified
    }

    public class SvnChange
    {
        public string FilePath { get; set; }
        public int OriginalLineNumber { get; set; }
        public string CodeSnippet { get; set; }
        public ChangeType ChangeType { get; set; }

    }

}
