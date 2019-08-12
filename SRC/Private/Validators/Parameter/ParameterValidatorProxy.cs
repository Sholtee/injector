/********************************************************************************
* ParameterValidatorProxy.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Proxy;

    internal class ParameterValidatorProxy<TInterface>: InterfaceInterceptor<TInterface> where TInterface: class
    {
        public ParameterValidatorProxy(TInterface target): base(target)
        {
        }

        public override object Invoke(MethodInfo targetMethod, object[] args, MemberInfo extra)
        {
            IReadOnlyList<Exception> validationErrors = targetMethod
                .GetParameters()
                .Select((param, i) => new
                {
                    param.Name,
                    Value = args[i],
                    param.GetCustomAttribute<ParameterIsAttribute>()?.Validators
                })
                .Where(param => param.Validators != null)
                .SelectMany
                (
                    param => param.Validators.SelectMany
                    (
                        validator => validator.Validate(param.Value, param.Name)
                    )
                )
                .ToArray();

            if (validationErrors.Any())
                throw validationErrors.Count == 1 ? validationErrors.Single() : new AggregateException(validationErrors);

            return base.Invoke(targetMethod, args, extra);
        }
    }
}
