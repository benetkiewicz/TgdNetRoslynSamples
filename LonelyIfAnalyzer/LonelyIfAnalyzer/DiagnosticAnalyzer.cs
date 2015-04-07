using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LonelyIfAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LonelyIfAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LonelyIfAnalyzer";
        internal const string Title = "If must be followed by block";
        internal const string MessageFormat = "If must be followed by block";
        internal const string Category = "Naming";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(ifaction, SyntaxKind.IfStatement);
        }

        private void ifaction(SyntaxNodeAnalysisContext context)
        {
            var ifstatement = context.Node as IfStatementSyntax;
            if (ifstatement !=null && ifstatement.Statement != null &&
                !ifstatement.Statement.IsKind(SyntaxKind.Block))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, ifstatement.Statement.GetLocation()));
            }
        }
    }
}
