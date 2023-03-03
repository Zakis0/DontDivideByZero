using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

class AssignmentNode {
    public readonly ISymbol VariableName;
    public readonly int? Value;

    public AssignmentNode(ISymbol variableName, int? value) {
        VariableName = variableName;
        Value = value;
    }
    public void PrintInfo() {
        Console.WriteLine(VariableName?.Name + ' ' + Value);
    }
}


 namespace DontDivideByZero {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DontDivideByZeroAnalyzer : DiagnosticAnalyzer {
        public const string DiagnosticId = "DontDivideByZero";
        
        private readonly List<AssignmentNode> _assignmentList = new List<AssignmentNode>();
        
        private static readonly LocalizableString Title = new LocalizableResourceString(
            nameof(Resources.AnalyzerTitle),
            Resources.ResourceManager, 
            typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
            nameof(Resources.AnalyzerMessageFormat), 
            Resources.ResourceManager, 
            typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(
            nameof(Resources.AnalyzerDescription),
            Resources.ResourceManager, 
            typeof(Resources));
        
        private const string Category = "Error";
        
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, 
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description
            );
        
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            
            context.RegisterSyntaxNodeAction(AddAssignmentNode, SyntaxKind.SimpleAssignmentExpression);
            context.RegisterSyntaxNodeAction(AddAssignmentNode, SyntaxKind.VariableDeclarator);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.DivideExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context) {
            var binaryExpression = (BinaryExpressionSyntax)context.Node;
            
            if (binaryExpression.Right.IsKind(SyntaxKind.IdentifierName)) {
                int? lastVarValue = null;
                foreach (AssignmentNode assignment in _assignmentList) {
                    if (assignment.Value == null) {
                        continue;
                    }
                    if (!SymbolEqualityComparer.Default.Equals(
                            assignment.VariableName,
                            context.SemanticModel.GetSymbolInfo(binaryExpression.Right, context.CancellationToken).Symbol
                        )) {
                        continue;
                    }
                    lastVarValue = assignment.Value;
                }
                if (lastVarValue == 0) {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
                }
            }
            if (context.SemanticModel.GetConstantValue(binaryExpression.Right).HasValue &&
                context.SemanticModel.GetConstantValue(binaryExpression.Right).Value.Equals(0))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
            }
        }

        private void AddAssignmentNode(SyntaxNodeAnalysisContext context) {
            if (context.Node.IsKind(SyntaxKind.VariableDeclarator)) {
                var tempDeclarator = (VariableDeclaratorSyntax)context.Node;
                int? value = null;
                
                if (tempDeclarator.Initializer != null && tempDeclarator.Initializer.Value.IsKind(SyntaxKind.NumericLiteralExpression)) {
                    value = (int)((LiteralExpressionSyntax)tempDeclarator.Initializer.Value).Token.Value;
                }
                
                _assignmentList.Add(new AssignmentNode(
                    context.SemanticModel.GetDeclaredSymbol(tempDeclarator, context.CancellationToken),
                    value
                ));
            }
            else if (context.Node.IsKind(SyntaxKind.SimpleAssignmentExpression)) {
                var tempAssignment = (AssignmentExpressionSyntax)context.Node;
                int? value = null;
                
                if (tempAssignment.Right.IsKind(SyntaxKind.NumericLiteralExpression)) {
                    value = (int)tempAssignment.Right.GetFirstToken().Value;
                }
                
                _assignmentList.Add(new AssignmentNode(
                    context.SemanticModel.GetSymbolInfo(tempAssignment.Left, context.CancellationToken).Symbol,
                    value
                ));
            }
        }
    }
}
