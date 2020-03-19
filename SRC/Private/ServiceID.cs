/********************************************************************************
* ServiceId.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal class ServiceId : IServiceId
    {
        public Type Interface { get; set; }

        public string? Name { get; set; }
    }
}
