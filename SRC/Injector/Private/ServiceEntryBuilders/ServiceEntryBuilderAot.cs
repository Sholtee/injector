/********************************************************************************
* ServiceEntryBuilderAot.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ServiceEntryBuilderAot : ServiceEntryBuilder
    {
        private readonly ServicePath FPath;

        private readonly ScopeOptions FScopeOptions;
#if !DEBUG
        private readonly ServiceRequestVisitor FVisitor;
#endif
        private Expression Visit(MethodCallExpression method, Expression target, Type iface, string? name)
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

                if (method.Method.Name == nameof(IInjector.TryGet))
                    //
                    // injector.[Try]Get(iface, name) -> (TInterface) null
                    //

                    return Expression.Default(iface);

                //
                // The injector.Get() method can be overridden in a permissive manner
                //

                if (FScopeOptions.SupportsServiceProvider)
                    return method;

                ServiceErrors.NotFound(iface, name, FPath?.Last);
            }

            //
            // injector.[Try]Get(iface, name) -> resolver.Resolve((IInstanceFactory) injector)
            //

            Expression resolve = Expression.Invoke
            (
                Expression.Constant((Func<IInstanceFactory, object>) resolver!.Resolve),
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

        public new const ServiceResolutionMode Id = ServiceResolutionMode.AOT;

        public ServiceEntryBuilderAot(IServiceResolverLookup lookup, ScopeOptions scopeOptions) : base(lookup)
        {
            FScopeOptions = scopeOptions;
            FPath = new ServicePath();
#if !DEBUG
            FVisitor = new ServiceRequestVisitor(Visit);
#endif
        }

        public override void Build(AbstractServiceEntry entry)
        {
            Debug.Assert(!entry.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (entry.State.HasFlag(ServiceEntryStateFlags.Built))
                return;
#if DEBUG
            int
                visitedRequests = 0,
                alteredRequests = 0;

            ServiceRequestVisitor FVisitor = new(VisitAndDebug);
#endif
            //
            // Throws if the request is circular
            //

            FPath.Push(entry);

            //
            // TODO: Enforce strict DI rules
            //

            try
            {
                entry.Build(lambda => (LambdaExpression) FVisitor.Visit(lambda));
            }
            finally
            {
                FPath.Pop();
            }

            //
            // TODO: Set the entry validated
            //
#if DEBUG
            Debug.WriteLine($"[{entry.ToString(shortForm: true)}] built: visited {visitedRequests}, altered {alteredRequests} requests");

            Expression VisitAndDebug(MethodCallExpression original, Expression target, Type iface, string? name)
            {
                visitedRequests++;

                Expression result = Visit(original, target, iface, name);
                if (result != original)
                    alteredRequests++;

                return result;
            }
#endif
        }
    }
}
