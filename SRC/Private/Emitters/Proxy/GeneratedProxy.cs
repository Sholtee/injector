/********************************************************************************
* GeneratedProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Properties;
    using Proxy;

    using static ProxyGeneratorBase;
    using static ProxyGenerator;

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

        public static string AssemblyName => GenerateAssemblyName(typeof(TInterceptor), typeof(TInterface));

        #region Private
        private static readonly object FLock = new object();

        // ReSharper disable once StaticMemberInGenericType
        private static Type FType;

        private static Type GenerateType()
        {
            CheckInterface();
            CheckBase();

            Assembly[] references = new HashSet<Assembly>
            (
                new[]
                {
                    typeof(Expression<>).Assembly(),
                    typeof(MethodInfo).Assembly(),
                    typeof(TInterface).Assembly(),
                    typeof(TInterceptor).Assembly()
                }
                .Concat(ReferencedAssemblies(typeof(TInterface).Assembly()))
                .Concat(ReferencedAssemblies(typeof(TInterceptor).Assembly())) // az interceptor konstruktora miatt lehetnek uj referenciak
            )
            .ToArray();

            return Compile
                .ToAssembly(GenerateProxyUnit(typeof(TInterceptor), typeof(TInterface)), AssemblyName, references)
                .GetType(GeneratedClassName, throwOnError: true);
        }

        private static Assembly AsAssembly(AssemblyName asm) => Assembly.Load(asm);

        private static IEnumerable<Assembly> ReferencedAssemblies(Assembly asm) => asm.GetReferencedAssemblies().Select(AsAssembly);

        private static void CheckInterface()
        {
            Type type = typeof(TInterface);

            CheckVisibility(type);

            if (!type.IsInterface()) throw new InvalidOperationException();
            if (type.ContainsGenericParameters()) throw new NotSupportedException();
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
            //
            // TODO: FIXME: privat tipusokra mindenkepp fel kene robbanjon (ha annotalva van az asm ha nincs).
            //

            if (type.IsNotPublic() && type.Assembly().GetCustomAttributes<InternalsVisibleToAttribute>().All(attr => attr.AssemblyName != AssemblyName)) throw new InvalidOperationException(string.Format(Resources.TYPE_NOT_VISIBLE, type));
        }
        #endregion
    }
}