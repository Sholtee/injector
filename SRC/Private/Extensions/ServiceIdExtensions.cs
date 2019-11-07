/********************************************************************************
* ServiceIdExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal static class ServiceIdExtensions
    {
        public static string FriendlyName(this (Type Interface, string Name) src) 
        {
            string result = src.Interface.ToString();
            if (src.Name != null) result += $":{src.Name}";
            return result;
        }
    }
}
