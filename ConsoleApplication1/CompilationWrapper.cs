using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    public class CompilationWrapper
    {
        public static byte[] CompileToBytes(params string[] sources)
        {
            var compilation = Compile(sources);

            EmitResult emitResult;

            using (var ms = new MemoryStream())
            {
                emitResult = compilation.Emit(ms);

                if (emitResult.Success)
                {
                    return ms.GetBuffer();
                }
            }

            var message = string.Join("\r\n", emitResult.Diagnostics);
            throw new ApplicationException(message);
        }

        public static CSharpCompilation Compile(params string[] sources)
        {
            var mscorlib = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var syntaxTrees = from source in sources
                             select CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("TestAsm", syntaxTrees, new[] { mscorlib }, compilationOptions);
            return compilation;
        }
    }
}
