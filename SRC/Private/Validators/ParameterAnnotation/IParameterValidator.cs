/********************************************************************************
* IParameterValidator.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    internal interface IParameterValidator
    {
        void Validate(object param, string paramName);
    }
}
