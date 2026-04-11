using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Rules.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RuleCategoryAttribute : Attribute
    {
        public string Category { get; }

        public RuleCategoryAttribute(string category)
        {
            Category = category;
        }
    }
}


