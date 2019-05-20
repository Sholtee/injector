/********************************************************************************
* Expect.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Solti.Utils.DI
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    internal sealed class ExpectAttribute: Attribute
    {
        private static readonly ConcurrentDictionary<Type, Func<IValidator>> Ctors = new ConcurrentDictionary<Type, Func<IValidator>>();

        public IValidator Validator { get; }

        public ExpectAttribute(Type validator)
        {
            Func<IValidator> ctor = Ctors.GetOrAdd
            (
                validator,
                @void => Expression.Lambda<Func<IValidator>>
                (
                    //
                    // return (IValidator) new Validator()
                    //

                    Expression.Convert(Expression.New(validator.GetConstructor(new Type[0])), typeof(IValidator))
                ).Compile()
            );

            Validator = ctor();
        }
    }
}
