﻿/********************************************************************************
* ServiceRequestReplacerVisitor.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;
    using Properties;

    /// <summary>
    /// Replaces the <see cref="IInjector.Get(Type, object?)"/> invocations with the corresponing <see cref="IServiceActivator.GetOrCreateInstance(AbstractServiceEntry)"/> calls.
    /// </summary>
    /// <remarks>This results a quicker delegate since it saves a <see cref="IServiceResolver.Resolve(Type, object?)"/> invocation for each dependency.</remarks>
    internal sealed class ServiceRequestReplacerVisitor : ServiceRequestVisitor, IFactoryVisitor
    {
        private static readonly MethodInfo FGetOrCreate = MethodInfoExtractor.Extract<IServiceActivator>(static fact => fact.GetOrCreateInstance(null!));

        private readonly IServiceResolver FServiceResolver;

        private readonly ServicePath FPath;

        private readonly bool FPermissive;

        public ServiceRequestReplacerVisitor(IServiceResolver resolver, ServicePath path, bool permissive)
        {
            FServiceResolver = resolver;
            FPath = path;
            FPermissive = permissive;
        }

        public LambdaExpression Visit(LambdaExpression factory, AbstractServiceEntry entry) => (LambdaExpression) Visit(factory);

        protected override Expression VisitServiceRequest(MethodCallExpression request, Expression scope, Type type, object? key)
        {
            if (scope.Type != typeof(IServiceActivator))
            {
                Trace.TraceWarning(Resources.REQUEST_NOT_REPLACEABLE);
                return request;
            }

            //
            // It specializes generic services ahead of time
            //

            AbstractServiceEntry? entry = FServiceResolver.Resolve(type, key);
            if (entry is null)
            {
                //
                // Missing but not required dependency
                //

                if (request.Method.Name == nameof(IInjector.TryGet) || FPermissive)
                    //
                    // injector.[Try]Get(iface, name) -> (TInterface) null
                    //

                    return Expression.Default(type);

                ServiceErrors.NotFound(type, key, FPath.Last);
            }

            //
            // injector.[Try]Get(iface, name) -> injector.GetOrCreateInstance(entry)
            //

            Expression resolve = Expression.Call
            (
                scope,
                FGetOrCreate,
                Expression.Constant(entry, typeof(AbstractServiceEntry))
            );

            return request.Method.ReturnType != type
                //
                // Cast was already done in the expression since the replaced IInjector.[Try]Get() method is not typed
                //

                ? resolve

                //
                // We are about to replace the typed IInjectorExtensions.[Try]Get() method so we need an extra cast.
                //

                : Expression.Convert
                (
                    resolve,
                    type
                );
        }
    }
}
