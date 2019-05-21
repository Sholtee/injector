/********************************************************************************
* IValidator.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    internal interface IValidator
    {
        void Validate(object param, string paramName);
    }
}
