/********************************************************************************
* ParameterValidator.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Workaround h vhogy lehessen generikusokat es attributomukat egyutt hasznalni.
    /// </summary>
    internal abstract class ParameterValidator<TParam> : IParameterValidator
    {
        void IParameterValidator.Validate(object param, string paramName) => Validate((TParam) param, paramName);

        protected abstract void Validate(TParam param, string paramName);
    }
}
