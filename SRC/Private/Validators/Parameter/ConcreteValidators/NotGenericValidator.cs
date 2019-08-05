/********************************************************************************
* NotGenericValidator.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class NotGeneric : ParameterValidator<Type>
    {
        protected override IEnumerable<Exception> Validate(Type param, string paramName)
        {
            if (param.IsGenericTypeDefinition())
                yield return new ArgumentException(Resources.CANT_INSTANTIATE_GENERICS, paramName);
        }
    }
}
