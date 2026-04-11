using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Analyzer.Rules.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

[RuleCategory("Stability")]
public class EmptyCatchRule : IRule
{
    public RuleMetadata Metadata => new RuleMetadata
    {
        RuleId = "R001",
        Title = "Avoid empty catch blocks",
        Description = "Catch blocks should not be empty. Handle or log the exception.",
        DefaultSeverity = Severity.Warning,
        Category = "Code Smell"
    };

    public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
    {
        if (context?.SyntaxRoot == null)
            yield break;

        var root = context.SyntaxRoot;

        var catchBlocks = root
            .DescendantNodes()
            .OfType<CatchClauseSyntax>()
            .Where(c => c.Block != null &&
                        !c.Block.Statements.Any());

        foreach (var catchBlock in catchBlocks)
        {
            var location = catchBlock.GetLocation().GetLineSpan();

            yield return new CodeIssue
            {
                RuleId = Metadata.RuleId,
                Message = "Empty catch block detected. Consider logging or handling the exception.",
                FilePath = context.FilePath,
                Line = location.StartLinePosition.Line + 1,
                Severity = Metadata.DefaultSeverity
            };
        }
    }
}