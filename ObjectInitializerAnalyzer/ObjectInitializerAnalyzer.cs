using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ObjectInitializerAnalyzer
{
    [DiagnosticAnalyzer(Microsoft.CodeAnalysis.LanguageNames.CSharp)]
    public class ObjectInitializerAnalyzer : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "TGD1";
        const string MessageFormat = "I hate OI";
        const string Category = "Naming";
        const string Title = "Do not use OI";
        DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
        

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeObjectInitializer, SyntaxKind.ObjectInitializerExpression);
        }

        private void AnalyzeObjectInitializer(SyntaxNodeAnalysisContext context)
        {
            if (context.Node == null)
            {
                return;
            }

            var expression = context.Node as InitializerExpressionSyntax;
            if (expression == null)
            {
                return;
            }

            if (expression.DescendantNodes().OfType<InitializerExpressionSyntax>().Any())
            {
                // do not support nested initializers
                return;
            }

            if (expression.Expressions.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, expression.GetLocation()));
            }
        }
    }
}
