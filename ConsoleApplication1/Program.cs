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

namespace ConsoleApplication1
{
    class Program
    {
        static string source = @"using System;
class Foo
{
    public static string Run()
    {
        return typeof(Foo).ToString();
    }
}";
        static void Main(string[] args)
        {
            // SyntaxTree st = CSharpSyntaxTree.Create(SyntaxFactory.ClassDeclaration("Example").WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))).NormalizeWhitespace());
            //Console.WriteLine(st.ToString());
            // FindControllersWithWalker();
            //FindControllersOfType();
            //SyntaxTreeIncorrect();
            //AppDomainRun();
            //SimpleCompilation();
            AccessibleField();
        }

        public static void AppDomainRun()
        {
            
            byte[] assembly = CompilationWrapper.CompileToBytes(source);
            //var type = assembly.GetType("Foo");
            //var method = type.GetMethod("Run");
            //var result = method.Invoke(null, new object[] { });

            //Console.WriteLine("Hurray, the expression returned " + result);
            var appDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString());
            appDomain.AssemblyResolve += AppDomain_AssemblyResolve;
            appDomain.Load(assembly);
            var foo = appDomain.CreateInstanceAndUnwrap("testAsm", "Foo");
        }

        private static Assembly AppDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.Load(CompilationWrapper.CompileToBytes(source));
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
                        if (IsMvcController(classSymbol))
                        {
                            Console.WriteLine(classSymbol);
                        }
                    }
                }
            }
        }

        private static bool IsMvcController(INamedTypeSymbol x)
        {
            var classBaseType = x.BaseType;
            if (classBaseType.ToString() == "object")
            {
                return false;
            }

            if (classBaseType.ToString() == "System.Web.Mvc.Controller")
            {
                return true;
            }

            return IsMvcController(classBaseType);
        }

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
                if (IsMvcController(symbol))
                {
                    Console.WriteLine(symbol);
                }

                base.VisitClassDeclaration(node);
            }
        }

        private static void SimpleCompilation()
        {
            string code = "class Test { void Foo() { } }";
            var mscorlib = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create("TestAsm", new[] { CSharpSyntaxTree.ParseText(code) }, new[] { mscorlib }, compilationOptions);
            foreach(var info in compilation.GetDiagnostics())
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
