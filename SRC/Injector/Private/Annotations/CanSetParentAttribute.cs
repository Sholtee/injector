/********************************************************************************
* CanSetParentAttribute.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class CanSetParentAttribute: Attribute
    {
    }
}
