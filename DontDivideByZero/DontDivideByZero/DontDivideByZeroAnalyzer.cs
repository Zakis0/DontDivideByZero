﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
 using Microsoft.CodeAnalysis.Text;

 namespace DontDivideByZero {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DontDivideByZeroAnalyzer : DiagnosticAnalyzer {
        public const string DiagnosticId = "DontDivideByZero";
        
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
        
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, 
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description
            );
        
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
            get {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.DivideExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context) {
            var binExp = (BinaryExpressionSyntax)context.Node;
            
            if (!context.SemanticModel.GetConstantValue(binExp.Right).Value.Equals(0)) {
                return;
            }
            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        }
    }
}
