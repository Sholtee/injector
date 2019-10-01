/********************************************************************************
* GeneratedProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Proxy;

    using static ProxyGeneratorBase;
    using static Compile;

    //
    // Statikus generikus azert jo nekunk mert igy biztosan pontosan egyszer fog lefutni az inicializacio minden egyes 
    // TBase-TInterface parosra. Ugyanez a Cache osztallyal nem lenne garantalhato: 
    //
    // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd?view=netcore-2.2
    //

    internal static class GeneratedProxy<TInterface, TInterceptor> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
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

        public static string AssemblyName => ProxyGenerator<TInterface, TInterceptor>.AssemblyName;

        #region Private
        private static readonly object FLock = new object();

        // ReSharper disable once StaticMemberInGenericType
        private static Type FType;

        private static Type GenerateType()
        {
            CheckInterface();
            CheckBase();

            Assembly[] references = new[]
            {
                typeof(Expression<>).Assembly(),
                typeof(MethodInfo).Assembly(),
                typeof(TInterface).Assembly(),
                typeof(TInterceptor).Assembly()
            }
            .Concat(typeof(TInterface).Assembly().GetReferences())
            .Concat(typeof(TInterceptor).Assembly().GetReferences()) // az interceptor konstruktora miatt lehetnek uj referenciak
            .Distinct()
            .ToArray();

            return ToAssembly
            (
                root: GenerateProxyUnit
                (
                    @class: ProxyGenerator<TInterface, TInterceptor>.GenerateProxyClass()
                ), 
                asmName: AssemblyName, 
                references: references
            )
            .GetType(GeneratedClassName, throwOnError: true);
        }

        private static void CheckInterface()
        {
            Type type = typeof(TInterface);

            CheckVisibility(type, AssemblyName);

            if (!type.IsInterface()) throw new InvalidOperationException();
            if (type.ContainsGenericParameters()) throw new NotSupportedException();
        }

        private static void CheckBase()
        {
            Type type = typeof(TInterceptor);

            CheckVisibility(type, AssemblyName);

            if (!type.IsClass()) throw new InvalidOperationException();
            if (type.ContainsGenericParameters()) throw new NotSupportedException();
            if (type.IsSealed()) throw new NotSupportedException();
        }
        #endregion
    }
}