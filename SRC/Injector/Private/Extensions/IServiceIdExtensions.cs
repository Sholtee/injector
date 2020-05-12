/********************************************************************************
* IServiceIdExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal static class IServiceIdExtensions
    {
        public static string FriendlyName(this IServiceId src) 
        {
            string result = src.Interface.ToString();
            if (src.Name != null) result += $":{src.Name}";
            return result;
        }
    }
}
