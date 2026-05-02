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
            Remediation = "Apply Single Responsibility Principle: split the class into focused services, each with one reason to change.",
            WhyItMatters = "God Classes accumulate all responsibilities, making them impossible to test in isolation, causing constant merge conflicts, and hiding bugs across unrelated features. Every change to the class is risky.",
            BadCodeExample =
                "// BAD: 500 lines, does authentication, email, payment, reporting...\n" +
                "class UserManager {\n" +
                "    public void Login() { }\n" +
                "    public void SendEmail() { }\n" +
                "    public void ProcessPayment() { }\n" +
                "    public void GeneratePdfReport() { }\n" +
                "    // ...30 more methods\n" +
                "}",
            GoodCodeExample =
                "// GOOD: each class has one reason to change\n" +
                "class AuthService      { public void Login() { } }\n" +
                "class NotificationSvc  { public void SendEmail() { } }\n" +
                "class PaymentService   { public void ProcessPayment() { } }\n" +
                "class ReportingService { public void GeneratePdf() { } }"
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
                    issues.Add(new CodeIssue
                    {
                        RuleId = Metadata.RuleId,
                        Message = BuildMessage(cls.Identifier.Text, methodCount, fieldCount, lineCount, complexity),
                        Line = cls.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        Severity = CalculateSeverity(methodCount, fieldCount, lineCount, complexity),
                        SuggestedFix = $"Split '{cls.Identifier.Text}' by responsibility: extract groups of related methods into dedicated service classes (AuthService, PaymentService, etc.)",
                        WhyItMatters = Metadata.WhyItMatters,
                        BadCodeExample = Metadata.BadCodeExample,
                        GoodCodeExample = Metadata.GoodCodeExample
                    });
                }
            }

            return issues;
        }

        private bool IsGodClass(int methods, int fields, int lines, int complexity)
        {
            // Bug 11 fix: AND was too strict — a class with 50 methods but 2 fields was never flagged.
            // Flag if at least 2 thresholds are exceeded.
            int exceeded = (methods > MethodThreshold ? 1 : 0)
                         + (fields > FieldThreshold ? 1 : 0)
                         + (lines > LineThreshold ? 1 : 0)
                         + (complexity > ComplexityThreshold ? 1 : 0);
            return exceeded >= 2;
        }

        private int GetLineCount(ClassDeclarationSyntax cls)
        {
            var span = cls.GetLocation().GetLineSpan();
            return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
        }

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
                   "Consider splitting responsibilities.";
        }
    }
}
