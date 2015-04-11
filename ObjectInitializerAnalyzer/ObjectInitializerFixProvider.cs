using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

namespace ObjectInitializerAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ObjectInitializerFixProvider))]
    class ObjectInitializerFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(ObjectInitializerAnalyzer.DiagnosticId);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declarations = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InitializerExpressionSyntax>();

            if (declarations == null)
                return;

            var declaration = declarations.First();

            context.RegisterCodeFix(
                CodeAction.Create("Rewrite Setters", c => RewriteSetters(context.Document, declaration, c)),
                diagnostic);
        }

        private async Task<Document> RewriteSetters(Document document, InitializerExpressionSyntax objectInitializer, CancellationToken c)
        {
            var root = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);
            var objectCreation = objectInitializer.Parent as ObjectCreationExpressionSyntax;
            var objectName = objectCreation.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            var objectCreationWithoutPropInit = objectCreation.RemoveNode(objectInitializer.Expressions.First(), SyntaxRemoveOptions.KeepNoTrivia);
            var separatePropertyInit = SyntaxFactory.ParseExpression(objectName + "." + objectInitializer.Expressions.First().ToString());
            var objectCreationWithTrailingPropInit = objectCreationWithoutPropInit.WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Comment("//" + separatePropertyInit.ToString())));
            var newroot =  root.ReplaceNode(objectCreation, objectCreationWithTrailingPropInit);
            return document.WithSyntaxRoot(newroot);
        }
    }
}
