/********************************************************************************
* TypeGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.DI.Internals
{
    using static ProxyGeneratorBase;

    //
    // Generikus osztaly azert jo nekunk mert igy biztosan pontosan egyszer fog lefutni az inicializacio minden egyes 
    // TDescendant-ra. Ugyanez a Cache osztallyal nem lenne garantalhato: 
    //
    // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd?view=netcore-2.2
    //

    internal abstract class TypeGenerator<TDescendant> : ITypeGenerator where TDescendant: TypeGenerator<TDescendant>
    {
        private static Type FType;

        private static readonly object FLock = new object();

        public Type Type
        {
            get
            {
                if (FType == null)
                    lock (FLock)
                        if (FType == null) FType = GenerateType();
                return FType;
            }
        }

        public abstract string AssemblyName { get; }

        protected abstract Type GenerateType();

        protected Type GenerateType(ClassDeclarationSyntax @class, IEnumerable<Assembly> references) => Compile
            .ToAssembly
            (
                root: GenerateProxyUnit(@class),
                asmName: AssemblyName,
                references: references.ToArray()
            )
            .GetType(GeneratedClassName, throwOnError: true);

        protected void CheckVisibility(Type type) => Compile.CheckVisibility(type, AssemblyName);
    }
}
