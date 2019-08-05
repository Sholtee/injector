/********************************************************************************
* NotNullValidator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal sealed class NotNull: ParameterValidator<object>
    {
        protected override IEnumerable<Exception> Validate(object param, string paramName)
        {
            if (param == null)
                yield return new ArgumentNullException(paramName);
        }
    }
}
