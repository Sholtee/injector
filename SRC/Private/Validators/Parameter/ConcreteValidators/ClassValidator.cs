/********************************************************************************
* ClassValidator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class Class: ParameterValidator<Type>
    {
        protected override IEnumerable<Exception> Validate(Type param, string paramName)
        {
            if (param == null) yield break;

            if (!param.IsClass())
                yield return new ArgumentException(Resources.NOT_A_CLASS, paramName);

            if (param.IsAbstract())
                yield return new ArgumentException(Resources.ABSTRACT, paramName);
        }
    }
}
