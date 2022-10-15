/********************************************************************************
* ServiceRequestReplacer.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal sealed class ServiceRequestReplacerVisitor : ServiceRequestVisitor, IFactoryVisitor
    {
        private readonly IServiceResolverLookup FLookup;

        private readonly ServicePath FPath;

        private readonly bool FPermissive;

        public ServiceRequestReplacerVisitor(IServiceResolverLookup lookup, ServicePath path, bool permissive)
        {
            FLookup = lookup;
            FPath = path;
            FPermissive = permissive;
        }

        public LambdaExpression Visit(LambdaExpression factory, AbstractServiceEntry entry) => (LambdaExpression) Visit(factory);

        protected override Expression VisitServiceRequest(MethodCallExpression request, Expression target, Type iface, string? name)
        {
            if (target.Type != typeof(IInstanceFactory))
            {
                Trace.TraceWarning(Resources.REQUEST_NOT_REPLACEABLE);
                return request;
            }

            //
            // It specializes generic services ahead of time
            //

            ServiceResolver? resolver = FLookup.Get(iface, name);
            if (resolver is null)
            {
                //
                // Missing but not required dependency
                //

                if (request.Method.Name == nameof(IInjector.TryGet) || FPermissive)
                    //
                    // injector.[Try]Get(iface, name) -> (TInterface) null
                    //

                    return Expression.Default(iface);

                ServiceErrors.NotFound(iface, name, FPath.Last);
            }

            //
            // injector.[Try]Get(iface, name) -> resolver.Resolve(injector)
            //

            Func<IInstanceFactory, object> resolveFn = resolver!.Resolve;

            Expression resolve = Expression.Invoke
            (
                Expression.Constant(resolveFn),
                target
            );

            return request.Method.ReturnType != iface
                //
                // Cast already in the expression since the replaced IInjector.[Try]Get() method is not typed
                //

                ? resolve

                //
                // We are about to replace the typed IInjectorExtensions.[Try]Get() method so we need an extra cast.
                //

                : Expression.Convert
                (
                    resolve,
                    iface
                );
        }
    }
}
