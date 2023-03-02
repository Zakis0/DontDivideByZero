using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

 namespace DontDivideByZero {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DontDivideByZeroAnalyzer : DiagnosticAnalyzer {
        public const string DiagnosticId = "DontDivideByZero";
        
        private readonly ArrayList _assignmentExpressions = new ArrayList();
        
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
            var binExp = (BinaryExpressionSyntax)context.Node;
            
            if (binExp.Right.IsKind(SyntaxKind.IdentifierName)) {
                int? lastVarValue = null;
                foreach (SyntaxNode assignment in _assignmentExpressions) {
                    if (assignment.IsKind(SyntaxKind.VariableDeclarator)) {
                        var tempDeclarator = (VariableDeclaratorSyntax)assignment;
                        
                        if (tempDeclarator.SpanStart > binExp.SpanStart) {
                            break;
                        }
                        if (tempDeclarator.Initializer == null) {
                            continue;
                        }

                        if (!tempDeclarator.GetFirstToken().Value.Equals(binExp.Right.GetFirstToken().Value)) {
                            continue;
                        }

                        lastVarValue = (int)((LiteralExpressionSyntax)tempDeclarator.Initializer.Value).Token.Value;
                    }
                    else if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)) {
                        var tempAssignment = (AssignmentExpressionSyntax)assignment;
                        
                        if (tempAssignment.SpanStart > binExp.SpanStart) {
                            break;
                        }
                        
                        if (!tempAssignment.Left.GetFirstToken().Value.Equals(binExp.Right.GetFirstToken().Value)) {
                            continue;
                        }
                        
                        lastVarValue = (int)tempAssignment.Right.GetFirstToken().Value;
                    }
                }
                if (lastVarValue == 0) {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
                }
            }
            if (context.SemanticModel.GetConstantValue(binExp.Right).HasValue &&
                context.SemanticModel.GetConstantValue(binExp.Right).Value.Equals(0))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
            }
        }

        private void AddAssignmentNode(SyntaxNodeAnalysisContext context) {
            _assignmentExpressions.Add(context.Node);
        }
    }
}
