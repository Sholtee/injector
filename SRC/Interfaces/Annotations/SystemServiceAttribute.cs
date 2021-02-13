/********************************************************************************
* SystemServiceAttribute.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    internal sealed class SystemServiceAttribute: Attribute
    {
    }
}
