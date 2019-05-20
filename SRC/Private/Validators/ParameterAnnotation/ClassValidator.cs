/********************************************************************************
* ClassValidator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Properties;

    internal sealed class Class: Validator<Type>
    {
        protected override void Validate(Type param, string paramName)
        {
            if (!param.IsClass) throw new ArgumentException(Resources.NOT_A_CLASS, paramName);
        }
    }
}
