using Analyzer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Core.Interface
{
    public interface IReportGenerator
    {
        Task GenerateAsync(
            IReadOnlyList<CodeIssue> issues,
            string outputPath);
    }
}
