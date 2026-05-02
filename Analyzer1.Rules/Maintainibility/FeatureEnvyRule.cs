using Analyzer.Rules.Attributes;
using global::Analyzer.Core.Enums;
using global::Analyzer.Core.Interfaces;
using global::Analyzer.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;


namespace Analyzer1.Rules.Maintainibility
{
    [RuleCategory("Maintainibility")]
   

    public class FeatureEnvyRule : IRule
        {
            private const int MinAccessThreshold = 4;
            private const double EnvyRatio = 1.5;

            public RuleMetadata Metadata => new RuleMetadata
            {
                RuleId = "DESIGN001",
                Title = "Feature Envy detected",
                Description = "Method relies more on external class data than its own class.",
                Category = "Design",
                DefaultSeverity = Severity.Info,
               // Remediation = "Move method to the class it heavily depends on."
            };

            public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
            {
                var issues = new List<CodeIssue>();

                if (context.SyntaxRoot == null || context.SemanticModel == null)
                    return issues;

                var methods = context.SyntaxRoot.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Body != null);

                foreach (var method in methods)
                {
                    var ownAccess = 0;
                    var externalAccessMap = new Dictionary<string, int>();

                    var memberAccesses = method.DescendantNodes()
                        .OfType<MemberAccessExpressionSyntax>();

                    foreach (var access in memberAccesses)
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(access).Symbol;

                        if (symbol == null)
                            continue;

                        var containingType = symbol.ContainingType;
                        if (containingType == null)
                            continue;

                        var currentClass = GetContainingClass(method, context);
                        if (currentClass == null)
                            continue;

                        var currentType = context.SemanticModel.GetDeclaredSymbol(currentClass);

                        // Own class access
                        if (SymbolEqualityComparer.Default.Equals(containingType, currentType))
                        {
                            ownAccess++;
                        }
                        else
                        {
                            var typeName = containingType.ToString();

                            if (!externalAccessMap.ContainsKey(typeName))
                                externalAccessMap[typeName] = 0;

                            externalAccessMap[typeName]++;
                        }
                    }

                    // Evaluate envy
                    foreach (var kvp in externalAccessMap)
                    {
                        var externalType = kvp.Key;
                        var externalAccess = kvp.Value;

                        if (externalAccess >= MinAccessThreshold &&
                            externalAccess > ownAccess * EnvyRatio)
                        {
                            issues.Add(new CodeIssue
                            {
                                RuleId = Metadata.RuleId,
                                Message = BuildMessage(method.Identifier.Text, externalType, ownAccess, externalAccess),
                                // Bug 9 fix: was 0-based
                                Line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                Severity = CalculateSeverity(externalAccess, ownAccess)
                            });
                        }
                    }
                }

                return issues;
            }

            private ClassDeclarationSyntax GetContainingClass(MethodDeclarationSyntax method, AnalysisContext context)
            {
                return method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            }

            private Severity CalculateSeverity(int external, int own)
            {
                if (external > own * 3)
                    return Severity.Critical;

                if (external > own * 2)
                    return Severity.Error;

                return Severity.Info;
            }

            private string BuildMessage(string methodName, string externalType, int own, int external)
            {
                return $"Method '{methodName}' shows Feature Envy towards '{externalType}' " +
                       $"(External access: {external}, Own access: {own}). Consider moving it.";
            }
        }
    
}
