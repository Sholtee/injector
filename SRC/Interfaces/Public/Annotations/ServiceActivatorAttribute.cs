/********************************************************************************
* ServiceActivatorAttribute.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Marks a constructor to be used by the injector. Useful in case of multiple constructors.
    /// <code>
    /// class MyService
    /// {
    ///     [ServiceActivator]
    ///     public MyService(IDependency dep) : this(dep, 1986) {...}
    ///     public MyService(IDependency dep, int foo) {...}
    /// }
    /// </code>
    /// </summary>
    /// <remarks>You can annotate only one constructor (per type).</remarks>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    public sealed class ServiceActivatorAttribute : Attribute
    {
    }
}
