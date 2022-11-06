/********************************************************************************
* Compiler.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    internal sealed class Compiler
    {
        private static IEnumerable<MetadataReference> GetReferences()
        {
            string[] platformAsms = new string[] { "netstandard.dll", "System.Runtime.dll" };

            string tpa = (string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");

            foreach (string asm in tpa.Split(Path.PathSeparator))
            {
                foreach (string platformAsm in platformAsms)
                {
                    if (Path.GetFileName(asm).Equals(platformAsm, StringComparison.OrdinalIgnoreCase))
                        yield return MetadataReference.CreateFromFile(asm);
                }
            }

            yield return MetadataReference.CreateFromFile(typeof(Compiler).Assembly.Location);
        }

        private readonly List<IRoslyCompilable> FCompilables = new();

        public void Compile(IRoslyCompilable compilable) => FCompilables.Add(compilable);

        public void Compile()
        {
            string className = $"Generated_{Guid.NewGuid():N}";

            List<Action<Type>> callbacks = new();

            Compilation compilation = CSharpCompilation.Create
            (
                assemblyName: className,
                syntaxTrees: new[]
                {
                    CSharpSyntaxTree.Create
                    (
                        CompilationUnit().WithMembers
                        (
                            List
                            (
                                SingletonList<MemberDeclarationSyntax>
                                (
                                    ClassDeclaration("Generated").WithMembers
                                    (
                                        List<MemberDeclarationSyntax>
                                        (
                                            FCompilables.Select
                                            (
                                                c =>
                                                {
                                                    MethodDeclarationSyntax method = c.BuildFactory(out object? closures);

                                                    callbacks.Add
                                                    (
                                                        type => c.CompletionCallback
                                                        (
                                                            type.InvokeMember
                                                            (
                                                                method.Identifier.ValueText,
                                                                BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                                                                null,
                                                                null,
                                                                new object[]
                                                                {
                                                                    closures!
                                                                },
                                                                null
                                                            )
                                                        )
                                                    );
                                                    
                                                    return method;
                                                }
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                },
                references: GetReferences(),
                options: new CSharpCompilationOptions
                (
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    metadataImportOptions: MetadataImportOptions.All,
                    optimizationLevel: OptimizationLevel.Release
                )
            );

            Debug.WriteLine
            (
                string.Join
                (
                    Environment.NewLine,
                    compilation
                        .SyntaxTrees
                        .Select(static unit => unit
                            .GetCompilationUnitRoot()
                            .NormalizeWhitespace(eol: Environment.NewLine)
                            .ToFullString())
                )
            );

            using MemoryStream stm = new();

            EmitResult result = compilation.Emit(stm);

            Debug.WriteLine(string.Join($",{Environment.NewLine}", result.Diagnostics));

            if (!result.Success)
            {
                string[]
                    failures = result
                        .Diagnostics
                        .Where(static d => d.Severity is not DiagnosticSeverity.Error)
                        .Select(static d => d.ToString())
                        .ToArray();

                InvalidOperationException ex = new("Compilation failed");

                IDictionary extra = ex.Data;

                extra.Add(nameof(failures), failures);

                throw ex;
            }

            stm.Seek(0, SeekOrigin.Begin);

            Type type = Assembly
                .Load
                (
                    stm.ToArray()
                )
                .GetType(className, throwOnError: true);

            FCompilables.Clear();
            callbacks.ForEach(cb => cb(type));
        }
    }
}
