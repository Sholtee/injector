/********************************************************************************
* ParameterIsAttribute.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class ParameterIsAttribute: Attribute
    {
        private static readonly ConcurrentDictionary<Type, IParameterValidator> Instances = new ConcurrentDictionary<Type, IParameterValidator>();

        public IReadOnlyList<IParameterValidator> Validators { get; }

        public ParameterIsAttribute(params Type[] validators)
        {
            Validators = validators.Select(validator => Instances.GetOrAdd
            (
                validator,
                @void => Expression.Lambda<Func<IParameterValidator>>
                (
                    //
                    // return (IValidator) new Validator()
                    //

                    Expression.Convert(Expression.New(validator.GetConstructor(new Type[0])), typeof(IParameterValidator))
                ).Compile()()
            )).ToArray();
        }
    }
}
