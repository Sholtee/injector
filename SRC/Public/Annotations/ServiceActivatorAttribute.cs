/********************************************************************************
* ServiceActivatorAttribute.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Annotations
{
    /// <summary>
    /// Marks a constructor to be used by the injector. Useful in case of multiple constructors.
    /// </summary>
    /// <remarks>You can annotate only one constructor.</remarks>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    public sealed class ServiceActivatorAttribute : Attribute
    {
    }
}
