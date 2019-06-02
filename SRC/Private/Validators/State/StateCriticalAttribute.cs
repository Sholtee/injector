/********************************************************************************
* StateCriticalAttribute.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class StateCriticalAttribute: Attribute
    {
    }
}
