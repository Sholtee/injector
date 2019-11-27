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

    internal sealed class GeneratedProxy<TInterface, TInterceptor> : TypeGenerator<GeneratedProxy<TInterface, TInterceptor>> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        public override string AssemblyName => ProxyGenerator<TInterface, TInterceptor>.AssemblyName;

        protected override Type GenerateType()
        {
            CheckInterface();
            CheckBase();

            return GenerateType
            (
                @class: ProxyGenerator<TInterface, TInterceptor>.GenerateProxyClass(),
                references: new[]
                {
                    typeof(Expression<>).Assembly(),
                    typeof(MethodInfo).Assembly(),
                }
                .Concat(typeof(TInterface).GetReferences())
                .Concat(typeof(TInterceptor).GetReferences()) // az interceptor konstruktora miatt lehetnek uj referenciak
                .Distinct()
            );
        }

        private void CheckInterface()
        {
            Type type = typeof(TInterface);

            CheckVisibility(type);

            if (!type.IsInterface()) throw new InvalidOperationException();
            if (type.ContainsGenericParameters()) throw new NotSupportedException();
        }

        private void CheckBase()
        {
            Type type = typeof(TInterceptor);

            CheckVisibility(type);

            if (!type.IsClass()) throw new InvalidOperationException();
            if (type.ContainsGenericParameters()) throw new NotSupportedException();
            if (type.IsSealed()) throw new NotSupportedException();
            if (type.IsAbstract()) throw new NotSupportedException();
        }
    }
}