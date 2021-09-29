/********************************************************************************
* InjectAttribute.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Marks a property to be resolved.
    /// </summary>
    /// <remarks>The target property must be writable.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class InjectAttribute : Attribute
    {
    }
}
