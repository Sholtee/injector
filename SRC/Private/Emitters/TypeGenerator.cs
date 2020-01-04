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
    using Properties;

    using static ProxyGeneratorBase;

    internal abstract class TypeGenerator
    {
        public static implicit operator Type(TypeGenerator self) => self.GeneratedType;

        public abstract string AssemblyName { get; }

        protected abstract Type GenerateType();

        protected abstract Type GeneratedType { get; }

        protected Type GenerateType(ClassDeclarationSyntax @class, IEnumerable<Assembly> references) => Compile
            .ToAssembly
            (
                root: GenerateProxyUnit(@class),
                asmName: AssemblyName,
                references: references.ToArray()
            )
            .GetType(GeneratedClassName, throwOnError: true);

        protected void CheckVisibility(Type type) 
        {
#if !IGNORE_VISIBILITY
            if (!Visibility.GrantedFor(type, AssemblyName))
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.TYPE_NOT_VISIBLE, type));
#endif
        }
    }

    //
    // Generikus osztaly azert jo nekunk mert igy biztosan pontosan egyszer fog lefutni az inicializacio minden egyes 
    // TDescendant-ra. Ugyanez a Cache osztallyal nem lenne garantalhato: 
    //
    // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd?view=netcore-2.2
    //

    internal abstract class TypeGenerator<TDescendant> : TypeGenerator where TDescendant : TypeGenerator<TDescendant>, new()
    {
        //
        // A lenti modszer bar mukodik de hiba eseten "TypeInitializationException"-t dob vissza
        // ami nekunk nem adja.
        //
        // public static Type Type = new TDescendant().GenerateType();
        //

        private static readonly object FLock = new object();

        private static Type FType;

        public static Type Type 
        {
            get 
            {
                if (FType == null)
                    lock (FLock)
                        if (FType == null)
                            FType = new TDescendant().GenerateType();
                return FType;
            }
        }

        protected sealed override Type GeneratedType => Type;
    }
}
