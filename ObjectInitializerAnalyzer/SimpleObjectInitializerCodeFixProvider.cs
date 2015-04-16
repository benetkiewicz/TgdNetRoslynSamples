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
using Microsoft.CodeAnalysis.Formatting;
using System.Composition;

namespace ObjectInitializerAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SimpleObjectInitializerCodeFixProvider)), Shared]
    public class SimpleObjectInitializerCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(SimpleObjectInitializerAnalyzer.DiagnosticId);
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
            var declarations = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>();

            if (declarations == null)
            {
                return;
            }

            var declaration = declarations.First();

            context.RegisterCodeFix(
                CodeAction.Create("Rewrite Setters", c => RewriteSetters(context.Document, declaration, c)),
                diagnostic);
        }

        private BlockSyntax GetContainingBlock(SyntaxNode node)
        {
            var block = node.Parent as BlockSyntax;
            if (block != null)
            {
                return block;
            }

            return GetContainingBlock(node.Parent);
        }

        private async Task<Document> RewriteSetters(Document document, LocalDeclarationStatementSyntax declarationWithInitializer, CancellationToken c)
        {
            var root = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);

            var variableDeclarator = declarationWithInitializer.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            string variableName = variableDeclarator.Identifier.ToString();

            var objectInitializer = declarationWithInitializer.DescendantNodes().OfType<InitializerExpressionSyntax>().First();
            var initializedProperties = new List<SyntaxNode>();
            foreach (var propInitialization in objectInitializer.Expressions)
            {
                var separatePropInitialization = SyntaxFactory.ParseStatement(variableName + "." + propInitialization.ToString() + ";");
                separatePropInitialization = separatePropInitialization.WithTrailingTrivia(SyntaxFactory.Whitespace(Environment.NewLine));
                initializedProperties.Add(separatePropInitialization);
            }

            var declarationWithoutInitializer = declarationWithInitializer.RemoveNode(objectInitializer, SyntaxRemoveOptions.KeepExteriorTrivia);
            declarationWithoutInitializer = declarationWithoutInitializer.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            var block = GetContainingBlock(declarationWithInitializer);
            var newBlock = block.TrackNodes(declarationWithInitializer);
            var refreshedObjectInitializer = newBlock.GetCurrentNode(declarationWithInitializer);
            newBlock = newBlock.InsertNodesAfter(refreshedObjectInitializer, initializedProperties).WithAdditionalAnnotations(Formatter.Annotation);
            refreshedObjectInitializer = newBlock.GetCurrentNode(declarationWithInitializer);
            newBlock = newBlock.ReplaceNode(refreshedObjectInitializer, declarationWithoutInitializer);

            var newroot = root.ReplaceNode(block, newBlock).WithAdditionalAnnotations(Formatter.Annotation);
            return document.WithSyntaxRoot(newroot);
        }
    }
}
