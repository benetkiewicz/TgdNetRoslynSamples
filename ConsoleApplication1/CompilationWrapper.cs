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
        public static byte[] Compile(params string[] sources)
        {
            var assemblyFileName = "testAsm";
            var compilation = CSharpCompilation.Create(assemblyFileName,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                syntaxTrees: from source in sources
                             select CSharpSyntaxTree.ParseText(source),
                references: new[]
        {
            MetadataReference.CreateFromAssembly(typeof(object).Assembly)
        });

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
    }
}
