/********************************************************************************
* ProducibleServiceEntry.Aspects.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;
    using Proxy.Generators;

    internal abstract partial class ProducibleServiceEntry
    {
        private Expression<Func<IInjector, Type, object, object>> BuildDelegate(Type interceptor)
        {
            interceptor = new ProxyGenerator(Interface, interceptor).GetGeneratedType();

            ConstructorInfo ctor = interceptor.GetApplicableConstructor();

            ParameterInfo? targetParam = null;

            foreach (ParameterInfo param in ctor.GetParameters())
            {
                if (param.ParameterType == Interface && param.GetCustomAttribute<OptionsAttribute>()?.Name is null)
                {
                    if (targetParam is not null)
                    {
                        //
                        // Target cannot be determined
                        //

                        targetParam = null;
                        break;
                    }

                    targetParam = param;            
                }
            }

            if (targetParam is null)
                throw new InvalidOperationException(Resources.TARGET_PARAM_CANNOT_BE_DETERMINED);

            return ServiceActivator.GetLateBound(ctor, targetParam.Position);
        }

        internal IEnumerable<Expression<Func<IInjector, Type, object, object>>> GetInterceptors(IEnumerable<AspectAttribute> aspects) // keep this method separated in order to seamless testing 
        {
            foreach (AspectAttribute aspect in aspects)
            {
                switch (aspect)
                {
                    case IInterceptorFactory<Type> typeFactory:
                        yield return BuildDelegate(typeFactory.GetInterceptor(Interface));
                        break;
                    case IInterceptorFactory<Expression<Func<IInjector, Type, object, object>>> expressionFactory:
                        yield return expressionFactory.GetInterceptor(Interface);
                        break;
                    default:
                        InvalidOperationException ex = new(Resources.INTERCEPTOR_FACTORY_NOT_IMPLEMENTED);
                        ex.Data[nameof(aspect)] = aspect;
                        throw ex;
                }
            }
        }

        public void ApplyAspects() 
        {
            foreach (Expression<Func<IInjector, Type, object, object>> interceptor in GetInterceptors(Interface.GetCustomAttributes<AspectAttribute>(inherit: true)))
            {
                ApplyProxy(interceptor);
            }
        }

        public void ApplyInterceptor(Type interceptor) => ApplyProxy
        (
            BuildDelegate(interceptor)
        );
    }
}
