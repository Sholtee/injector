/********************************************************************************
* GeneratedDuck.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    using static ProxyGeneratorBase;

    internal static class GeneratedDuck<TInterface, TTarget> where TInterface: class
    {
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

        public static string AssemblyName => DuckGenerator<TTarget>.GenerateAssemblyName<TInterface>();

        #region Private
        private static readonly object FLock = new object();

        // ReSharper disable once StaticMemberInGenericType
        private static Type FType;

        private static Type GenerateType()
        {
            CheckInterface();

            Assembly[] references = new HashSet<Assembly>
            (
                new[]
                {
                    typeof(TInterface).Assembly(),
                    typeof(TTarget).Assembly()
                }
                .Concat(typeof(TInterface).Assembly().GetReferences())
            )
            .ToArray();

            return Compile
                .ToAssembly
                (
                    root: GenerateProxyUnit
                    (
                        @class: DuckGenerator<TTarget>.GenerateDuckClass<TInterface>()
                    ), 
                    asmName: AssemblyName, 
                    references: references
                )
                .GetType(GeneratedClassName, throwOnError: true);
        }

        private static void CheckInterface()
        {
            Type type = typeof(TInterface);

            CheckVisibility(type);

            if (!type.IsInterface()) throw new InvalidOperationException();
            if (type.ContainsGenericParameters()) throw new NotSupportedException();
        }

        private static void CheckVisibility(Type type)
        {
            if (type.IsNotPublic()) throw new InvalidOperationException(string.Format(Resources.TYPE_NOT_VISIBLE, type));
        }
        #endregion
    }
}