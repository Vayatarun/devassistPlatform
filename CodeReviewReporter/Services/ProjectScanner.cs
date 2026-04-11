using Analyzer.Core.Models;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace CodeReviewReporter.Services
{
    
        [Analyzer.Rules.Attributes.RuleCategory("Security")] 

          public class ProjectScanner
        {
            public async Task<List<AnalysisContext>> ScanSolutionAsync(string solutionPath)
            {
                var contexts = new List<AnalysisContext>();

                // 🔹 Load MSBuild
                MSBuildLocator.RegisterDefaults();

                using var workspace = MSBuildWorkspace.Create();

                var solution = await workspace.OpenSolutionAsync(solutionPath);

                foreach (var project in solution.Projects)
                {
                    var projectContexts = await ScanProjectAsync(project);
                    contexts.AddRange(projectContexts);
                }

                return contexts;
            }

            private async Task<List<AnalysisContext>> ScanProjectAsync(Microsoft.CodeAnalysis.Project project)
            {
                var contexts = new List<AnalysisContext>();

                var compilation = await project.GetCompilationAsync();

                foreach (var document in project.Documents)
                {
                    if (!document.SupportsSyntaxTree)
                        continue;

                    var syntaxTree = await document.GetSyntaxTreeAsync();
                    if (syntaxTree == null)
                        continue;

                    var root = await syntaxTree.GetRootAsync();
                    var semanticModel = compilation.GetSemanticModel(syntaxTree);

                    var source = root.ToFullString();

                    contexts.Add(new AnalysisContext
                    {
                        FilePath = document.FilePath,
                        SourceCode = source,
                        SyntaxRoot = root,
                        SemanticModel = semanticModel
                    });
                }

                return contexts;
            }
        }
    }

