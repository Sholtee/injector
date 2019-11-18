/********************************************************************************
* Compile.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Immutable;
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

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal static class Compile
    {
        public static void CheckVisibility(Type type, string asmName)
        {
#if !IGNORE_VISIBILITY
            //
            // Mivel az "internal" es "protected" kulcsszavak nem leteznek IL szinten ezert itt reflexioval
            // nem tudjuk megallapitani h a tipus lathato e a kodunk szamara =(
            //

            CompilationUnitSyntax unitToCheck = CompilationUnit().WithUsings
            (
                usings: SingletonList
                (
                    UsingDirective
                    (
                        name: (NameSyntax) ProxyGeneratorBase.CreateType(type)
                    )
                    .WithAlias
                    (
                        alias: NameEquals(IdentifierName("t"))
                    )
                )
            );

            Debug.WriteLine(unitToCheck.NormalizeWhitespace().ToFullString());

            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName: asmName,
                syntaxTrees: new[]
                {
                    CSharpSyntaxTree.Create
                    (
                        root: unitToCheck
                    )
                },
                references: RuntimeAssemblies
                    .Concat(type.GetReferences())
                    .Distinct()
                    .Select(asm => MetadataReference.CreateFromFile(asm.Location)),
                options: CompilationOptionsFactory.Create()
            );

            ImmutableArray<Diagnostic> diagnostics = compilation.GetDeclarationDiagnostics();
            if (diagnostics.Any(diag => diag.Severity == DiagnosticSeverity.Error))
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.TYPE_NOT_VISIBLE, type));
#endif
        }

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
                options: CompilationOptionsFactory.Create()
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