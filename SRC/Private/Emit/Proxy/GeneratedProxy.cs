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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Solti.Utils.DI.Internals
{
    using Properties;
    using Proxy;

    //
    // Statikus generikus azert jo nekunk mert igy biztosan pontosan egyszer fog lefutni az inicializacio minden egyes 
    // TBase-TInterface parosra. Ugyanez a Cache osztallyal nem lenne garantalhato: 
    //
    // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd?view=netcore-2.2
    //

    internal static class GeneratedProxy<TInterface, TInterceptor> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        private static readonly object FLock = new object();

        // ReSharper disable once StaticMemberInGenericType
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

        public static string AssemblyName => ProxyGenerator.GenerateAssemblyName(typeof(TInterceptor), typeof(TInterface));

        #region Private
        private static Type GenerateType()
        {
            CheckInterface();
            CheckBase();

            SyntaxTree tree = CSharpSyntaxTree.Create
            (
                root: ProxyGenerator.GenerateProxyUnit(typeof(TInterceptor), typeof(TInterface))
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
                assemblyName: AssemblyName,
                syntaxTrees: new [] {tree},
                references: ReferencedAssemblies().Select(asm => MetadataReference.CreateFromFile(asm)),
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
                    .LoadFromStream(stm)
                    .GetType(ProxyGenerator.GeneratedClassName, throwOnError: true);
            } 
        }

        private static Assembly AsAssembly(AssemblyName asm) => Assembly.Load(asm);

        private static IEnumerable<Assembly> ReferencedAssemblies(Assembly asm) => asm.GetReferencedAssemblies().Select(AsAssembly);
#if NETSTANDARD
        private static IEnumerable<Assembly> RuntimeAssemblies()
        {
            string[] 
                trustedAssembliesPaths = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator),
                neededAssemblies = 
                {
                    "System.Runtime",
                    "netstandard"
                };

            return trustedAssembliesPaths
                .Where(path => neededAssemblies.Contains(Path.GetFileNameWithoutExtension(path)))
                //
                // Assembly.LoadFile() nincs NetStandard 2 alatt
                //

                .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath);
        }
#endif
        private static IEnumerable<string> ReferencedAssemblies() => new HashSet<Assembly>
        (
            new []
            {
                typeof(Object).Assembly(), // explicit meg kell adni
                typeof(Expression<>).Assembly(),
                typeof(MethodInfo).Assembly(),
#if IGNORE_VISIBILITY
                typeof(IgnoresAccessChecksToAttribute).Assembly(),
#endif
                typeof(TInterface).Assembly(),
                typeof(TInterceptor).Assembly()
            }
#if NETSTANDARD
            .Concat(RuntimeAssemblies())
#endif
            .Concat(ReferencedAssemblies(typeof(TInterface).Assembly()))
            .Concat(ReferencedAssemblies(typeof(TInterceptor).Assembly())) // az interceptor konstruktora miatt lehetnek uj referenciak
        )
        .Select(asm => asm.Location);

        private static void CheckInterface()
        {
            Type type = typeof(TInterface);

            CheckVisibility(type);

            if (!type.IsInterface()) throw new InvalidOperationException();
            if (type.ContainsGenericParameters()) throw new NotSupportedException();
            if (type.GetEvents().Any()) throw new NotSupportedException();
        }

        private static void CheckBase()
        {
            Type type = typeof(TInterceptor);

            CheckVisibility(type);

            if (!type.IsClass()) throw new InvalidOperationException();
            if (type.ContainsGenericParameters()) throw new NotSupportedException();
            if (type.IsSealed()) throw new NotSupportedException();
        }

        private static void CheckVisibility(Type type)
        {
            if (type.IsNested) throw new NotSupportedException(Resources.TYPE_IS_NESTED);

            //
            // TODO: FIXME: privat tipusokra mindenkepp fel kene robbanjon (ha annotalva van az asm ha nincs).
            //

            if (type.IsNotPublic() && type.Assembly().GetCustomAttributes<InternalsVisibleToAttribute>().All(attr => attr.AssemblyName != AssemblyName)) throw new InvalidOperationException(string.Format(Resources.TYPE_NOT_VISIBLE, type));
        }
        #endregion
    }
}