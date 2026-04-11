using CodeReviewReporter.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeReviewReporter.Services
{
    public class SvnCommitAnalyzer
    {
      
            public List<CommitChange> ParseDiff(string diffText)
            {
                var changes = new List<CommitChange>();

                string currentFile = "";
                int newLineNumber = 0;

                var lines = diffText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                foreach (var line in lines)
                {
                    // Detect file
                    if (line.StartsWith("Index:"))
                    {
                        currentFile = line.Replace("Index:", "").Trim();
                        continue;
                    }

                    // Detect hunk @@ -10,6 +10,8 @@
                    if (line.StartsWith("@@"))
                    {
                        var match = Regex.Match(line, @"\+(\d+)");

                        if (match.Success)
                            newLineNumber = int.Parse(match.Groups[1].Value);

                        continue;
                    }

                    // Added line
                    if (line.StartsWith("+") && !line.StartsWith("+++"))
                    {
                        changes.Add(new CommitChange
                        {
                            FileName = currentFile,
                            LineNumber = newLineNumber,
                            ChangeType = "Added",
                            Code = line.Substring(1)
                        });

                        newLineNumber++;
                    }

                    // Deleted line
                    else if (line.StartsWith("-") && !line.StartsWith("---"))
                    {
                        changes.Add(new CommitChange
                        {
                            FileName = currentFile,
                            LineNumber = newLineNumber,
                            ChangeType = "Deleted",
                            Code = line.Substring(1)
                        });
                    }

                    // Unchanged line
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(line) &&
                            !line.StartsWith("===") &&
                            !line.StartsWith("---") &&
                            !line.StartsWith("+++"))
                        {
                            changes.Add(new CommitChange
                            {
                                FileName = currentFile,
                                LineNumber = newLineNumber,
                                ChangeType = "Unchanged",
                                Code = line
                            });

                            newLineNumber++;
                        }
                    }
                }

                return changes;
            }

        public string ExtractAddedCode(string diff)
        {
            var lines = diff.Split('\n');

            var builder = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.StartsWith("+") && !line.StartsWith("+++"))
                {
                    builder.AppendLine(line.Substring(1));
                }
            }

            return builder.ToString();
        }

        public List<(string file, string code)> ExtractCodeFromDiff(string diff)
        {
            var result = new List<(string, string)>();

            string currentFile = "";
            var builder = new System.Text.StringBuilder();

            foreach (var line in diff.Split('\n'))
            {
                if (line.StartsWith("Index:"))
                {
                    if (builder.Length > 0)
                    {
                        result.Add((currentFile, builder.ToString()));
                        builder.Clear();
                    }

                    currentFile = line.Replace("Index:", "").Trim();
                }

                if (line.StartsWith("+") && !line.StartsWith("+++"))
                {
                    builder.AppendLine(line.Substring(1));
                }
            }

            if (builder.Length > 0)
                result.Add((currentFile, builder.ToString()));

            return result;
        }
    }


    


}

