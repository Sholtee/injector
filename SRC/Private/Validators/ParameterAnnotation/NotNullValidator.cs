/********************************************************************************
* NotNullValidator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal sealed class NotNull: Validator<object>
    {
        protected override void Validate(object param, string paramName)
        {
            if (param == null)
                throw new ArgumentNullException(paramName);
        }
    }
}
