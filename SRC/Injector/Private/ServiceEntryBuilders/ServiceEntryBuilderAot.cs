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

    internal sealed class ServiceEntryBuilderAot: ServiceEntryBuilder
    {
        private readonly ServicePath FPath;

        private readonly ServiceRequestVisitor FRequestVisitor;

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
                // Do NOT throw here as the injector.Get() method can be overridden in a permissive manner
                //

                return method;
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

        public ServiceEntryBuilderAot(IServiceResolverLookup lookup): base(lookup)
        {
            FPath = new ServicePath();
            FRequestVisitor = new ServiceRequestVisitor(Visit);
        }

        public override void Build(AbstractServiceEntry entry)
        {
            Debug.Assert(!entry.Interface.IsGenericTypeDefinition, "Generic entry cannot be built");

            if (entry.State.HasFlag(ServiceEntryStateFlags.Built))
                return;

            //
            // Throws if the request is circular
            //

            FPath.Push(entry);
            try
            {
                entry.Build(lambda => (LambdaExpression) FRequestVisitor.Visit(lambda));
            }
            finally
            {
                FPath.Pop();
            }
        }
    }
}
