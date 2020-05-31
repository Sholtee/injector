/********************************************************************************
* ParameterValidator.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Extensions.Aspects
{
    using Proxy;

    /// <summary>
    /// Defines a generic parameter validator proxy.
    /// </summary>
    /// <remarks>You should never instantiate this class directly.</remarks>
    public class ParameterValidator<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
    {
        /// <summary>
        /// Creates a new <see cref="ParameterValidator{TInterface}"/> instance.
        /// </summary>
        public ParameterValidator(TInterface target) : base(target ?? throw new ArgumentNullException(nameof(target)))
        {
        }

        /// <summary>
        /// See <see cref="InterfaceInterceptor{TInterface}.Invoke(MethodInfo, object[], MemberInfo)"/>.
        /// </summary>
        public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (args == null)
                throw new ArgumentNullException(nameof(args));

            foreach (var ctx in method.GetParameters().Select(
                (p, i) => new
                {
                    Parameter = p,
                    Value = args[i],
                    Validators = p.GetCustomAttributes<ParameterValidatorAttribute>()
                }))
            {
                foreach (var validator in ctx.Validators)
                {
                    validator.Validate(ctx.Parameter, ctx.Value);
                }
            }

            return base.Invoke(method, args, extra);
        }
    }
}
