/********************************************************************************
* InterfaceValidator.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Properties;

    internal sealed class Interface: Validator<Type>
    {
        protected override void Validate(Type param, string paramName)
        {
            if (!param.IsInterface) throw new ArgumentException(Resources.NOT_AN_INTERFACE, paramName);
        }
    }
}
