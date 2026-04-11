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
    public class GodClassRule : IRule
        {
            private const int MethodThreshold = 20;
            private const int FieldThreshold = 10;
            private const int LineThreshold = 300;
            private const int ComplexityThreshold = 15;

            public RuleMetadata Metadata => new RuleMetadata
            {
                RuleId = "DESIGN002",
                Title = "God Class detected",
                Description = "Class has too many responsibilities, making it hard to maintain.",
                Category = "Design",
                DefaultSeverity = Severity.Info,
               // Remediation = "Break the class into smaller, focused classes."
            };

            public IEnumerable<CodeIssue> Analyze(AnalysisContext context)
            {
                var issues = new List<CodeIssue>();

                if (context.SyntaxRoot == null)
                    return issues;

                var classes = context.SyntaxRoot.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>();

                foreach (var cls in classes)
                {
                    var methods = cls.Members.OfType<MethodDeclarationSyntax>().ToList();
                    var fields = cls.Members.OfType<FieldDeclarationSyntax>().ToList();

                    int methodCount = methods.Count;
                    int fieldCount = fields.Count;
                    int lineCount = GetLineCount(cls);
                    int complexity = CalculateComplexity(cls);

                    if (IsGodClass(methodCount, fieldCount, lineCount, complexity))
                    {
                        var severity = CalculateSeverity(methodCount, fieldCount, lineCount, complexity);

                        issues.Add(new CodeIssue
                        {
                            RuleId = Metadata.RuleId,
                            Message = BuildMessage(cls.Identifier.Text, methodCount, fieldCount, lineCount, complexity),
                            Line = cls.GetLocation().GetLineSpan().StartLinePosition.Line,
                            Severity = severity
                        });
                    }
                }

                return issues;
            }

            private bool IsGodClass(int methods, int fields, int lines, int complexity)
            {
                return methods > MethodThreshold &&
                       fields > FieldThreshold &&
                       (lines > LineThreshold || complexity > ComplexityThreshold);
            }

            private int GetLineCount(ClassDeclarationSyntax cls)
            {
                var span = cls.GetLocation().GetLineSpan();
                return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
            }

            // 🔥 Simple complexity (can upgrade to cognitive later)
            private int CalculateComplexity(ClassDeclarationSyntax cls)
            {
                return cls.DescendantNodes().Count(n =>
                    n is IfStatementSyntax ||
                    n is ForStatementSyntax ||
                    n is WhileStatementSyntax ||
                    n is ForEachStatementSyntax ||
                    n is SwitchStatementSyntax ||
                    n is CatchClauseSyntax);
            }

            private Severity CalculateSeverity(int methods, int fields, int lines, int complexity)
            {
                if (methods > 40 || fields > 20 || lines > 600)
                    return Severity.Critical;

                if (methods > 30 || lines > 450)
                    return Severity.Error;

                return Severity.Info;
            }

            private string BuildMessage(string className, int methods, int fields, int lines, int complexity)
            {
                return $"Class '{className}' appears to be a God Class " +
                       $"(Methods: {methods}, Fields: {fields}, Lines: {lines}, Complexity: {complexity}). " +
                       $"Consider splitting responsibilities.";
            }
        }
    }

