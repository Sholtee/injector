/********************************************************************************
* IServiceIdExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class IServiceIdExtensions
    {
        public static string FriendlyName(this IServiceId src) => $"[{src.Name ?? "NULL"}] {src.Interface}";
    }
}
