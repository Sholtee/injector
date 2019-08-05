/********************************************************************************
* IParameterValidator.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal interface IParameterValidator
    {
        IEnumerable<Exception> Validate(object param, string paramName);
    }
}
