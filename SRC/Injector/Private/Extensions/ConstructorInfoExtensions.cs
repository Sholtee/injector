/********************************************************************************
* ConstructorInfoExtensions.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Proxy.Internals;

    internal static class ConstructorInfoExtensions
    {
        public static IReadOnlyList<ParameterInfo> GetParametersSafe(this ConstructorInfo self)
        {
            IReadOnlyList<ParameterInfo> paramz = self.GetParameters();

            if (self.ReflectedType.GetCustomAttribute<RelatedGeneratorAttribute>() is not null)
            {
                //
                // Specialis eset amikor generalt proxy tipussal dolgozunk. Itt bar a konstruktorok szama es elrendezese
                // megegyezik az os tipussal de a parameterekrol hianyoznak az attributumok azert az os konstruktoranak 
                // parametereit adjuk vissza.
                //

                ConstructorInfo? baseCtor = self.ReflectedType.BaseType.GetConstructor
                (
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    paramz.Select(param => param.ParameterType).ToArray(),
                    null
                );

                if (baseCtor is not null)
                    return baseCtor.GetParameters();
            }

            return paramz;
        }
    }
}
