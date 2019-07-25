/********************************************************************************
* ClassValidator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class Class: ParameterValidator<Type>
    {
        protected override void Validate(Type param, string paramName)
        {
            if (!param.IsClass())
                throw new ArgumentException(Resources.NOT_A_CLASS, paramName);

            if (param.IsAbstract())
                throw new ArgumentException(Resources.ABSTRACT, paramName);
        }
    }
}
