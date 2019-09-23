/********************************************************************************
* IServiceCollection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal interface IServiceCollection: ICollection<AbstractServiceEntry>
    {
        bool TryGet(Type iface, out AbstractServiceEntry entry);
        bool TryGetClosest(Type iface, out AbstractServiceEntry entry);
    }
}
