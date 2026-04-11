using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer.Core.Security
{
    

        /// <summary>
        /// Tracks tainted variables inside a method (Level-1: intra-method only)
        /// </summary>
        public class TaintTrackingEngine
        {
            private readonly SemanticModel _semanticModel;

            // Stores tainted variables
            private readonly HashSet<ISymbol> _taintedSymbols = new(SymbolEqualityComparer.Default);

            public TaintTrackingEngine(SemanticModel semanticModel)
            {
                _semanticModel = semanticModel;
            }

            public IReadOnlyCollection<ISymbol> TaintedSymbols => _taintedSymbols;

            // Entry point per method
            public void AnalyzeMethod(MethodDeclarationSyntax method)
            {
                if (method.Body == null)
                    return;

                foreach (var statement in method.Body.Statements)
                {
                    ProcessStatement(statement);
                }
            }

            // 🔹 Process statements
            private void ProcessStatement(StatementSyntax statement)
            {
                switch (statement)
                {
                    case LocalDeclarationStatementSyntax localDecl:
                        HandleLocalDeclaration(localDecl);
                        break;

                    case ExpressionStatementSyntax exprStmt:
                        HandleExpression(exprStmt.Expression);
                        break;

                    case IfStatementSyntax ifStmt:
                        ProcessStatement(ifStmt.Statement);
                        if (ifStmt.Else != null)
                            ProcessStatement(ifStmt.Else.Statement);
                        break;

                    case BlockSyntax block:
                        foreach (var stmt in block.Statements)
                            ProcessStatement(stmt);
                        break;
                }
            }

            // 🔹 Handle variable declarations
            private void HandleLocalDeclaration(LocalDeclarationStatementSyntax localDecl)
            {
                foreach (var variable in localDecl.Declaration.Variables)
                {
                    var symbol = _semanticModel.GetDeclaredSymbol(variable);

                    if (variable.Initializer == null || symbol == null)
                        continue;

                    var value = variable.Initializer.Value;

                    if (IsSource(value) || IsTainted(value))
                    {
                        _taintedSymbols.Add(symbol);
                    }
                }
            }

            // 🔹 Handle expressions (assignments, calls)
            private void HandleExpression(ExpressionSyntax expr)
            {
                if (expr is AssignmentExpressionSyntax assignment)
                {
                    var leftSymbol = _semanticModel.GetSymbolInfo(assignment.Left).Symbol;
                    var right = assignment.Right;

                    if (leftSymbol == null)
                        return;

                    if (IsSource(right) || IsTainted(right))
                    {
                        _taintedSymbols.Add(leftSymbol);
                    }
                    else if (IsSanitized(right))
                    {
                        _taintedSymbols.Remove(leftSymbol);
                    }
                }
                else if (expr is InvocationExpressionSyntax invocation)
                {
                    HandleInvocation(invocation);
                }
            }

            // 🔹 Handle method calls
            private void HandleInvocation(InvocationExpressionSyntax invocation)
            {
                var methodSymbol = _semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (methodSymbol == null)
                    return;

                // If any argument is tainted → propagation
                var args = invocation.ArgumentList.Arguments;

                bool hasTaintedArg = args.Any(arg => IsTainted(arg.Expression));

                if (hasTaintedArg)
                {
                    // Assign taint to variable if assigned
                    var parent = invocation.Parent as AssignmentExpressionSyntax;
                    if (parent != null)
                    {
                        var leftSymbol = _semanticModel.GetSymbolInfo(parent.Left).Symbol;
                        if (leftSymbol != null)
                            _taintedSymbols.Add(leftSymbol);
                    }
                }
            }

            // 🔴 SOURCE detection
            private bool IsSource(ExpressionSyntax expr)
            {
                var text = expr.ToString().ToLower();

                return text.Contains("request") ||
                       text.Contains("query") ||
                       text.Contains("form") ||
                       text.Contains("readline");
            }

            // 🟡 Check if expression is already tainted
            private bool IsTainted(ExpressionSyntax expr)
            {
                var symbol = _semanticModel.GetSymbolInfo(expr).Symbol;
                return symbol != null && _taintedSymbols.Contains(symbol);
            }

            // 🟢 Sanitizer detection
            private bool IsSanitized(ExpressionSyntax expr)
            {
                var text = expr.ToString().ToLower();

                return text.Contains("sanitize") ||
                       text.Contains("encode") ||
                       text.Contains("parameter");
            }

            // 🔵 Sink check (used by rules)
            public bool IsSink(InvocationExpressionSyntax invocation)
            {
                var text = invocation.ToString().ToLower();

                return text.Contains("execute") ||
                       text.Contains("sql") ||
                       text.Contains("process.start") ||
                       text.Contains("write");
            }

            // 🔥 Check if sink is dangerous
            public bool IsTaintedSink(InvocationExpressionSyntax invocation)
            {
                return invocation.ArgumentList.Arguments
                    .Any(arg => IsTainted(arg.Expression));
            }
        }
    }

