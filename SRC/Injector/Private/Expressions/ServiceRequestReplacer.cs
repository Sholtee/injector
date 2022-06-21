/********************************************************************************
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
        private readonly IServiceResolverLookup FLookup;

        private readonly ServicePath FPath;

        private readonly bool FPermissive;

        public ServiceRequestReplacer(IServiceResolverLookup lookup, ServicePath path, bool permissive)
        {
            FLookup = lookup;
            FPath = path;
            FPermissive = permissive;
        }

        protected override Expression VisitServiceRequest(MethodCallExpression method, Expression target, Type iface, string? name)
        {
            //
            // It specializes generic services ahead of time
            //

            IServiceResolver? resolver = FLookup.Get(iface, name);
            if (resolver is null)
            {
                //
                // Missing but not required dependency
                //

                if (method.Method.Name == nameof(IInjector.TryGet) || FPermissive)
                    //
                    // injector.[Try]Get(iface, name) -> (TInterface) null
                    //

                    return Expression.Default(iface);

                ServiceErrors.NotFound(iface, name, FPath.Last);
            }

            //
            // injector.[Try]Get(iface, name) -> resolver.Resolve((IInstanceFactory) injector)
            //

            Expression resolve = resolver!.GetResolveExpression
            (
                Expression.Convert(target, typeof(IInstanceFactory))
            );

            return method.Method.ReturnType != iface
                //
                // Cast already in the expression since the replaced IInjector.[Try]Get() method is not typed
                //

                ? resolve

                //
                // We are about to replace the typed IInjectorExtensions.[Try]Get() method so we will need an extry cast.
                //

                : Expression.Convert
                (
                    resolve,
                    iface
                );
        }
    }
}
