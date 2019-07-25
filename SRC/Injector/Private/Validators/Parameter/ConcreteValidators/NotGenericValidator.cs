/********************************************************************************
* NotGenericValidator.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class NotGeneric : ParameterValidator<Type>
    {
        protected override void Validate(Type param, string paramName)
        {
            if (param.IsGenericTypeDefinition())
                throw new ArgumentException(Resources.CANT_INSTANTIATE_GENERICS, paramName);
        }
    }
}
