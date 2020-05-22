/********************************************************************************
* ServiceId.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ServiceId : IServiceId
    {
        public ServiceId(Type @interface, string? name) 
        {
            Interface = @interface;
            Name = name;
        }

        public Type Interface { get; }

        public string? Name { get; }
    }
}
