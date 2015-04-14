using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.MSBuild;
using System.Reflection;
using ConsoleApplication1.Walkers;

namespace ConsoleApplication1
{
    class Foo
    {
        class X
        {
            public X X2 { get; set; }
            public int MyProperty { get; set; }

        }
        static void Main(string[] args)
        {
            ThreeMethods();
            // SyntaxTreeAPI();
            // FindControllersWithWalker();
            //FindControllersOfType();
            //SyntaxTreeIncorrect();
            //SimpleCompilation();
            //AccessibleField();
        }

        public static void ThreeMethods()
        {
            string code = "class Foo { public Foo() {} public int Bar { get; set; } public string Baz { get; set; } }";
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            SyntaxNode root = syntaxTree.GetRoot();
            Console.WriteLine("IsKind() ==================");
            foreach (var node in root.DescendantNodes())
            {
                if (node.IsKind(SyntaxKind.PropertyDeclaration))
                {
                    Console.WriteLine(node);
                }
            }

            Console.WriteLine("OfType<>() ==================");
            var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var property in properties)
            {
                Console.WriteLine(property);
            }

            Console.WriteLine("Walker ==================");
            new PropertiesWalker().Visit(root);
        }

        public static void SyntaxTreeAPI()
        {
            var x = new X() { MyProperty = 123 };
            SyntaxTree st = CSharpSyntaxTree.Create(SyntaxFactory.ClassDeclaration("Example").WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))).NormalizeWhitespace());
            Console.WriteLine(st.ToString());
        }

        public static void SyntaxTreeIncorrect()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText("class Test { void Foo() { } int a }"); // TODO: contains diagnostics?
            var root = syntaxTree.GetRoot();
            Console.WriteLine(root);
        }

        public static void FindControllersWithWalker()
        {
            var workSpace = MSBuildWorkspace.Create();
            var solution = workSpace.OpenSolutionAsync(@"C:\Users\piotratais\Documents\Visual Studio 2015\Projects\RoslynTarget\RoslynTarget.sln").Result;
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var syntaxTree = document.GetSyntaxTreeAsync().Result;
                    var semanticModel = document.GetSemanticModelAsync().Result;
                    new ClassWalker(semanticModel).Visit(syntaxTree.GetRoot());
                }
            }

        }

        public static void FindControllersOfType()
        {
            var workSpace = MSBuildWorkspace.Create();
            var solution = workSpace.OpenSolutionAsync(@"C:\Users\piotratais\Documents\Visual Studio 2015\Projects\RoslynTarget\RoslynTarget.sln").Result;
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var syntaxTree = document.GetSyntaxTreeAsync().Result;
                    var model = document.GetSemanticModelAsync().Result;
                    var classSyntaxNodes = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
                    foreach (var classSyntaxNode in classSyntaxNodes)
                    {
                        var classSymbol = model.GetDeclaredSymbol(classSyntaxNode);
                        if (RoslynHelper.IsMvcController(classSymbol))
                        {
                            Console.WriteLine(classSymbol);
                        }
                    }
                }
            }
        }

        private static void SimpleCompilation()
        {
            string code = "class Test { void Foo() { } }";
            var mscorlib = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create("TestAsm", new[] { CSharpSyntaxTree.ParseText(code) }, new[] { mscorlib }, compilationOptions);
            foreach (var info in compilation.GetDiagnostics())
            {
                Console.WriteLine(info);
            }
        }

        private static void AccessibleField()
        {
            string code = @"
public class Foo 
{ 
    protected string Bar { get; set; } 
}
public class Baz : Foo
{
    public void DoWork()
    {
        System.Console.WriteLine(Bar);
    }
}
";
            var mscorlib = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create("TestAsm", new[] { syntaxTree }, new[] { mscorlib }, compilationOptions);
            foreach (var info in compilation.GetDiagnostics())
            {
                Console.WriteLine(info);
            }
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var invocations = syntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
            var barArgument = invocations[0].ArgumentList.Arguments[0].Expression as IdentifierNameSyntax;
            var barSymbol = semanticModel.GetSymbolInfo(barArgument);
        }
    }
}
