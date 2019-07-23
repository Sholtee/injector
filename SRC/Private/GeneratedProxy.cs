/********************************************************************************
* GeneratedProxy.cs                                                             *
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    //
    // Statikus generikus azert jo nekunk mert igy biztosan pontosan egyszer fog lefutni az inicializacio minden egyes 
    // TBase-TInterface parosra. Ugyanez a Cache osztallyal nem lenne garantalhato: 
    //
    // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd?view=netcore-2.2
    //

    internal static class GeneratedProxy<TInterface, TBase> 
    {
        private static readonly object FLock = new object();

        private static Type FType;

        public static Type Type
        {
            get
            {
                if (FType == null)
                    lock (FLock)
                        if (FType == null) FType = GenerateType();
                return FType;
            }
        }

        private static Type GenerateType()
        {
            CheckInterface();
            CheckBase();

            SyntaxTree tree = CSharpSyntaxTree.Create
            (
                root: CompilationUnit().WithMembers
                (
                    members: SingletonList<MemberDeclarationSyntax>(ProxyGenerator.GenerateProxyClass(typeof(TBase), typeof(TInterface)))
                )
            );
#if DEBUG
            if (Debugger.IsAttached)
            {
                Debug.WriteLine(tree.GetRoot().NormalizeWhitespace().ToFullString());
                Debug.WriteLine(string.Join(Environment.NewLine, ReferencedAssemblies()));
            }
#endif
            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName: Path.GetRandomFileName(),
                syntaxTrees: new [] {tree},
                references: ReferencedAssemblies().Select(asm => MetadataReference.CreateFromFile(asm)),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using (var stm = new MemoryStream())
            {
                EmitResult result = compilation.Emit(stm);

                if (!result.Success)
                {
                    IReadOnlyList<Diagnostic> failures = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

                    throw new Exception("Compilation failed");
                }

                stm.Seek(0, SeekOrigin.Begin);

                Assembly asm = AssemblyLoadContext.Default.LoadFromStream(stm);

                return asm.GetType(ProxyGenerator.GeneratedClassName, throwOnError: true);
            } 
        }

        private static Assembly AsAssembly(AssemblyName asm) => Assembly.Load(asm);

        private static IEnumerable<Assembly> ReferencedAssemblies(Assembly asm) => asm.GetReferencedAssemblies().Select(AsAssembly);

        private static IEnumerable<string> ReferencedAssemblies() => new HashSet<Assembly>(ReferencedAssemblies(typeof(TInterface).Assembly))
        {
            typeof(Object).Assembly, // explicit meg kell adni
            typeof(TBase).Assembly   // TBase szerelvenye mar lehet szerepel -> HashSet
        }
        .Select(asm => asm.Location);

        private static void CheckInterface()
        {
            Type type = typeof(TInterface);

            if (!type.IsInterface) throw new InvalidOperationException();
            if (type.IsNested) throw new NotSupportedException();
            if (!type.IsPublic) throw new NotSupportedException();
            if (type.ContainsGenericParameters) throw new NotSupportedException();
        }

        private static void CheckBase()
        {
            Type type = typeof(TBase);

            if (!type.IsClass) throw new InvalidOperationException();
            if (type.IsNested) throw new NotSupportedException();
            if (!type.IsPublic) throw new NotSupportedException();
            if (type.ContainsGenericParameters) throw new NotSupportedException();
            if (type.IsSealed) throw new NotSupportedException();
        }
    }
}