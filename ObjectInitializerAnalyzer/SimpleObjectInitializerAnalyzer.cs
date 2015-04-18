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
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SimpleObjectInitializerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TGD1";
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
            context.RegisterSyntaxNodeAction(AnalyzeObjectInitializer, SyntaxKind.LocalDeclarationStatement);
        }

        private void AnalyzeObjectInitializer(SyntaxNodeAnalysisContext context)
        {
            var localDeclarationExpression = context.Node as LocalDeclarationStatementSyntax;
            if (localDeclarationExpression == null)
            {
                return;
            }

            if (localDeclarationExpression.Declaration.Variables.Count != 1)
            {
                return;
            }

            var innerObjectInitializers = localDeclarationExpression.DescendantNodes().OfType<InitializerExpressionSyntax>().ToList();
            if (innerObjectInitializers.Count != 1)
            {
                return;
            }

            var objectInitializer = innerObjectInitializers[0];
            context.ReportDiagnostic(Diagnostic.Create(Rule, objectInitializer.GetLocation()));
        }
    }
}
