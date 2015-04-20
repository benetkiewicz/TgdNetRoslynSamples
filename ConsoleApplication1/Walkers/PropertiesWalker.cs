using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgdNet.Walkers
{
    class PropertiesWalker : CSharpSyntaxWalker
    {
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            Console.WriteLine(node + " // from walker");
        }
    }
}
