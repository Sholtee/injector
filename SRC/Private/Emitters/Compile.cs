/********************************************************************************
* Compile.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

#if IGNORE_VISIBILITY
using System.Runtime.CompilerServices;
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal static class Compile
    {
        public static Assembly ToAssembly(CompilationUnitSyntax root, string asmName, params Assembly[] references)
        {
            Debug.WriteLine(root.NormalizeWhitespace().ToFullString());
            Debug.WriteLine(string.Join<Assembly>(Environment.NewLine, references));
 
            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName: asmName,
                syntaxTrees: new []
                {
                    CSharpSyntaxTree.Create
                    (
                        root: root
                    )
                },
                references: RuntimeAssemblies
                    .Concat(references)
#if IGNORE_VISIBILITY
                    .Append(typeof(IgnoresAccessChecksToAttribute).Assembly())
#endif
                    .Select(asm => MetadataReference.CreateFromFile(asm.Location))
                ,
                options: CompilationOptionsFactory.Create
                (
#if IGNORE_VISIBILITY
                    ignoreAccessChecks: true
#else
                    ignoreAccessChecks: false
#endif
                )
            );

            using (Stream stm = new MemoryStream())
            {
                EmitResult result = compilation.Emit(stm);

                Debug.WriteLine(string.Join(Environment.NewLine, result.Diagnostics));

                if (!result.Success)
                {
                    Diagnostic[] failures = result
                        .Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .ToArray();

                    var ex = new Exception(Resources.COMPILATION_FAILED);
                    ex.Data.Add(nameof(failures), failures);

                    throw ex;
                }

                stm.Seek(0, SeekOrigin.Begin);

                return AssemblyLoadContext
                    .Default
                    .LoadFromStream(stm);
            } 
        }

        private static readonly Assembly[] RuntimeAssemblies = GetRuntimeAssemblies().ToArray();

        private static IEnumerable<Assembly> GetRuntimeAssemblies()
        {
            yield return typeof(Object).Assembly();
#if NETSTANDARD
            string[] mandatoryAssemblies = 
            {
                "System.Runtime",
                "netstandard"
            };

            foreach (string assemblyPath in ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator))
            {
                if (mandatoryAssemblies.Contains(Path.GetFileNameWithoutExtension(assemblyPath)))
                    yield return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
            }
#endif
        }
    }
}