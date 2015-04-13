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

        private BlockSyntax GetContainingBlock(SyntaxNode node)
        {
            var block = node.Parent as BlockSyntax;
            if (block != null)
            {
                return block;
            }

            return GetContainingBlock(node.Parent);
        }

        private LocalDeclarationStatementSyntax GetContainingLocalDeclaration(SyntaxNode syntaxNode)
        {
            var localDeclaration = syntaxNode.Parent as LocalDeclarationStatementSyntax;
            if (localDeclaration != null)
            {
                return localDeclaration;
            }

            return GetContainingLocalDeclaration(syntaxNode.Parent);
        }

        private async Task<Document> RewriteSetters(Document document, InitializerExpressionSyntax objectInitializer, CancellationToken c)
        {
            var root = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);

            var block = GetContainingBlock(objectInitializer);
            var localDeclaration = GetContainingLocalDeclaration(objectInitializer);
            var variableDeclarator = localDeclaration.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            string variableName = variableDeclarator.Identifier.ToString();

            var initializedProperties = new List<SyntaxNode>();
            foreach (var propInitialization in objectInitializer.Expressions)
            {
                var separatePropInitialization = SyntaxFactory.ParseStatement(variableName + "." + propInitialization.ToString() + ";");
                separatePropInitialization = separatePropInitialization.WithTrailingTrivia(SyntaxFactory.Whitespace(Environment.NewLine));
                initializedProperties.Add(separatePropInitialization);
            }

            var newBlock = block.InsertNodesAfter(localDeclaration, initializedProperties);
            var refreshedObjectInitializer = newBlock.DescendantNodes().OfType<InitializerExpressionSyntax>().First();
            var newBlock2 = newBlock.RemoveNode(refreshedObjectInitializer, SyntaxRemoveOptions.KeepEndOfLine);
            
            var newroot = root.ReplaceNode(block, newBlock2).WithAdditionalAnnotations(Formatter.Annotation);
            return document.WithSyntaxRoot(newroot);
        }
    }
}
