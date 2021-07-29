/********************************************************************************
* IHasValidationMethod.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Diagnostics
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHasValidationMethod
    {
        /// <summary>
        /// Defines a custom logic (e.g.: testing the data connection) to be called to validate the service.
        /// </summary>
        void Validate();
    }
}
