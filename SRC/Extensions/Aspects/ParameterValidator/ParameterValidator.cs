/********************************************************************************
* ParameterValidator.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Extensions.Aspects
{
    using Interfaces;
    using Proxy;

    /// <summary>
    /// Defines a generic parameter validator proxy.
    /// </summary>
    /// <remarks>You should never instantiate this class directly.</remarks>
    public class ParameterValidator<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
    {
        /// <summary>
        /// Specifies that the validation should return all the validation errors.
        /// </summary>
        public bool Aggregate { get; }

        /// <summary>
        /// Creates a new <see cref="ParameterValidator{TInterface}"/> instance.
        /// </summary>
        [ServiceActivator]
        public ParameterValidator(TInterface target) : this(target, false) { }

        /// <summary>
        /// Creates a new <see cref="ParameterValidator{TInterface}"/> instance.
        /// </summary>
        public ParameterValidator(TInterface target, bool aggregate) : base(target ?? throw new ArgumentNullException(nameof(target))) =>
            Aggregate = aggregate;

        /// <summary>
        /// See <see cref="InterfaceInterceptor{TInterface}.Invoke(MethodInfo, object[], MemberInfo)"/>.
        /// </summary>
        public override object? Invoke(MethodInfo method, object[] args, MemberInfo extra)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (args == null)
                throw new ArgumentNullException(nameof(args));

            var validationErrors = new List<ArgumentException>();

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
                    try
                    {
                        validator.Validate(ctx.Parameter, ctx.Value);
                    }
                    catch (ArgumentException validationError) 
                    {
                        if (!Aggregate) throw;
                        validationErrors.Add(validationError);
                    }
                }
            }

            if (validationErrors.Any())
                throw new AggregateException(validationErrors);

            return base.Invoke(method, args, extra);
        }
    }
}
