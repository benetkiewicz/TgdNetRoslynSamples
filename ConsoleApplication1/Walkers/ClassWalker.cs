using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgdNet.Walkers
{
    class ClassWalker : CSharpSyntaxWalker
    {
        private SemanticModel semanticModel;

        public ClassWalker(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var symbol = this.semanticModel.GetDeclaredSymbol(node);
            if (RoslynHelper.IsMvcController(symbol))
            {
                Console.WriteLine(symbol);
            }

            base.VisitClassDeclaration(node);
        }
    }
}
