/********************************************************************************
* InterfaceValidator.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class Interface: ParameterValidator<Type>
    {
        protected override IEnumerable<Exception> Validate(Type param, string paramName)
        {
            if (!param.IsInterface())
                yield return new ArgumentException(Resources.NOT_AN_INTERFACE, paramName);
        }
    }
}
