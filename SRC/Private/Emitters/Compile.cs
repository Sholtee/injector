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
            SyntaxTree tree = CSharpSyntaxTree.Create
            (
                root: root
            );
#if DEBUG
            if (Debugger.IsAttached)
            {
                Debug.WriteLine(tree.GetRoot().NormalizeWhitespace().ToFullString());
                Debug.WriteLine(string.Join<Assembly>(Environment.NewLine, references));
            }
#endif
            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName: asmName,
                syntaxTrees: new [] {tree},
                references: references
#if NETSTANDARD
                    .Concat(RuntimeAssemblies)
#endif
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

                if (!result.Success)
                {
                    IReadOnlyList<Diagnostic> failures = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

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
#if NETSTANDARD
        private static readonly Assembly[] RuntimeAssemblies = GetRuntimeAssemblies();

        private static Assembly[] GetRuntimeAssemblies()
        {
            string[] 
                trustedAssembliesPaths = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator),
                mandatoryAssemblies = 
                {
                    "System.Runtime",
                    "netstandard"
                };

            return trustedAssembliesPaths
                .Where(path => mandatoryAssemblies.Contains(Path.GetFileNameWithoutExtension(path)))
                //
                // Assembly.LoadFile() nincs NetStandard 2 alatt
                //

                .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                .ToArray();
        }
#endif
    }
}