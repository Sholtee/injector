/********************************************************************************
* MemberInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    internal static class MemberInfoExtensions
    {
        public static string GetFullName(this MemberInfo src) => $"{ProxyGeneratorBase.CreateType(src.DeclaringType).ToFullString()}.{src.Name}";
    }
}
