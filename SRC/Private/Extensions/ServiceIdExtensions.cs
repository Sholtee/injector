﻿/********************************************************************************
* ServiceIdExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal static class ServiceIdExtensions
    {
        public static string FriendlyName(this IServiceID src) 
        {
            string result = src.Interface.ToString();
            if (src.Name != null) result += $":{src.Name}";
            return result;
        }
    }
}
