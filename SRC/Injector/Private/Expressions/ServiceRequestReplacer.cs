﻿/********************************************************************************
* ServiceRequestReplacer.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ServiceRequestReplacer : ServiceRequestVisitor
    {
        public IServiceResolverLookup Lookup { get; }

        public ServicePath Path { get; }

        public bool Permissive { get; }

        public ServiceRequestReplacer(IServiceResolverLookup lookup, ServicePath path, bool permissive)
        {
            Lookup = lookup;
            Path = path;
            Permissive = permissive;
        }

        protected override Expression VisitServiceRequest(MethodCallExpression method, Expression target, Type iface, string? name)
        {
            //
            // It specializes generic services ahead of time
            //

            IServiceResolver? resolver = Lookup.Get(iface, name);
            if (resolver is null)
            {
                //
                // Missing but not required dependency
                //

                if (method.Method.Name == nameof(IInjector.TryGet) || Permissive)
                    //
                    // injector.[Try]Get(iface, name) -> (TInterface) null
                    //

                    return Expression.Default(iface);

                ServiceErrors.NotFound(iface, name, Path.Last);
            }

            //
            // injector.[Try]Get(iface, name) -> resolver.Resolve((IInstanceFactory) injector)
            //

            Expression resolve = resolver!.GetResolveExpression
            (
                Expression.Convert(target, typeof(IInstanceFactory))
            );

            return method.Method.ReturnType != iface
                ? resolve

                //
                // As IInjectorExtensions.[Try]Get() is a typed method (method.ReturnType == iface), this cast would be
                // redundant.
                //

                : Expression.Convert
                (
                    resolve,
                    iface
                );
        }
    }
}
