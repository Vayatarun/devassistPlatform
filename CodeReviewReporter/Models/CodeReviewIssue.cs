using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeReviewReporter.Models
{
    namespace CodeReviewReporter.Models
    {
        public class CodeReviewIssue
        {
            public string FileName { get; set; }
            public int LineNumber { get; set; }
            public string RuleId { get; set; }
            public string Severity { get; set; }
            public string Message { get; set; }
            public string Description { get; set; }
            public string Code { get; set; }
            public string SuggestedFix { get; set; }

            public string Category { get; set; }



        }


    }
}
