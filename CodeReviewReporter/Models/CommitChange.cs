using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeReviewReporter.Models
{
    public class CommitChange
    {
        public string FileName { get; set; }

        public int LineNumber { get; set; }

        public string ChangeType { get; set; } // Added / Deleted / Unchanged

        public string Code { get; set; }
    }

}
