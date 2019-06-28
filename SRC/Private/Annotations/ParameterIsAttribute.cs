/********************************************************************************
* ParameterIsAttribute.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class ParameterIsAttribute: Attribute
    {
        public IReadOnlyList<IParameterValidator> Validators { get; }

        public ParameterIsAttribute(params Type[] validators)
        {
            Validators = validators.Select(validator => Cache<Type, IParameterValidator>.GetOrAdd
            (
                validator,
                () => (IParameterValidator) validator.CreateInstance(new Type[0])
            )).ToArray();
        }
    }
}
