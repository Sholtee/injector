/********************************************************************************
* ParameterValidator.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Workaround h vhogy lehessen generikusokat es attributomukat egyutt hasznalni.
    /// </summary>
    internal abstract class ParameterValidator<TParam> : IParameterValidator
    {
        IEnumerable<Exception> IParameterValidator.Validate(object param, string paramName) => Validate((TParam) param, paramName);

        protected abstract IEnumerable<Exception> Validate(TParam param, string paramName);
    }
}
