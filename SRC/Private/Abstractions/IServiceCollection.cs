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
        AbstractServiceEntry Get(Type iface);
        bool TryGetClosest(Type iface, out AbstractServiceEntry entry);
        AbstractServiceEntry GetClosest(Type iface);
        AbstractServiceEntry Query(Type iface);
    }
}
