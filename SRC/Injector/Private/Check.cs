/********************************************************************************
* Check.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Injector
{
    internal static class Check
    {
        public static void NotNull(object member, string name)
        {
            if (member == null) throw new ArgumentNullException(name);
        }
    }
}
