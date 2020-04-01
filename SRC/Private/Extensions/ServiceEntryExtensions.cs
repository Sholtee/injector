/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal static class ServiceEntryExtensions
    {
        public static bool IsGeneric(this AbstractServiceEntry entry) => entry.Interface.IsGenericTypeDefinition;
    }
}
