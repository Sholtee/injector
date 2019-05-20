/********************************************************************************
* Validator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI
{
    /// <summary>
    /// Workaround h vhogy lehessen generikusokat es attributomukat egyutt hasznalni.
    /// </summary>
    internal abstract class Validator<TParam> : IValidator
    {
        void IValidator.Validate(object param, string paramName)
        {
            Validate((TParam) param, paramName);
        }

        protected abstract void Validate(TParam param, string paramName);
    }
}
