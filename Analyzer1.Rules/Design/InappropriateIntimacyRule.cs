using Analyzer.Core.Enums;
using Analyzer.Core.Interfaces;
using Analyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Analyzer.Rules.Attributes;
namespace Analyzer.Rules.Design
{

    [RuleCategory("Design")]
    public class InappropriateIntimacyRule : IRule
    {
        private const int AccessThreshold = 6;
        private const int DistinctMemberThreshold = 4;

        public RuleMetadata Metadata => new RuleMetadata
        {
            RuleId = "DESIGN003",
            Title = "Inappropriate Intimacy detected",
            Description = "Class is too tightly coupled with another class.",
            Category = "Design",
            DefaultSeverity = Severity.Major,
            //Remediation = "Reduce coupling by introducing interfaces or moving logic."
        };

        public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
        {
            var issues = new List<CodeIssue>();

            if (context.SyntaxRoot == null || context.SemanticModel == null)
                return issues;

            var classes = context.SyntaxRoot.DescendantNodes()
                .OfType<ClassDeclarationSyntax>();

            foreach (var cls in classes)
            {
                var dependencyMap = new Dictionary<string, List<string>>();

                var memberAccesses = cls.DescendantNodes()
                    .OfType<MemberAccessExpressionSyntax>();

                foreach (var access in memberAccesses)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(access).Symbol;
                    if (symbol == null)
                        continue;

                    var containingType = symbol.ContainingType;
                    if (containingType == null)
                        continue;

                    var currentType = context.SemanticModel.GetDeclaredSymbol(cls);
                    if (currentType == null)
                        continue;

                    // Skip own class
                    if (SymbolEqualityComparer.Default.Equals(containingType, currentType))
                        continue;

                    var typeName = containingType.ToString();
                    var memberName = symbol.Name;

                    if (!dependencyMap.ContainsKey(typeName))
                        dependencyMap[typeName] = new List<string>();

                    dependencyMap[typeName].Add(memberName);
                }

                // Evaluate dependencies
                foreach (var kvp in dependencyMap)
                {
                    var externalType = kvp.Key;
                    var accesses = kvp.Value;

                    int totalAccess = accesses.Count;
                    int distinctMembers = accesses.Distinct().Count();

                    if (totalAccess >= AccessThreshold &&
                        distinctMembers >= DistinctMemberThreshold)
                    {
                        issues.Add(new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = BuildMessage(cls.Identifier.Text, externalType, totalAccess, distinctMembers),
                            Line = cls.GetLocation().GetLineSpan().StartLinePosition.Line,
                            Severity = CalculateSeverity(totalAccess, distinctMembers)
                        });
                    }
                }
            }

            return issues;
        }

        private Severity CalculateSeverity(int totalAccess, int distinctMembers)
        {
            if (totalAccess > 15 && distinctMembers > 8)
                return Severity.Critical;

            if (totalAccess > 10)
                return Severity.Major;

            return Severity.Minor;
        }

        private string BuildMessage(string className, string externalType, int total, int distinct)
        {
            return $"Class '{className}' is highly coupled with '{externalType}' " +
                   $"(Accesses: {total}, Distinct members: {distinct}). Consider reducing dependency.";
        }
    }
}