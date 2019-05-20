/********************************************************************************
* IValidator.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI
{
    internal interface IValidator
    {
        void Validate(object param, string paramName);
    }
}
