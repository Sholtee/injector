/********************************************************************************
* ServiceReferenceHolder.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal class ServiceReferenceHolder : IServiceID
    {
        public Type Interface { get; set; }

        public string Name { get; set; }

        public ServiceReference Value { get; set; }
    }
}
