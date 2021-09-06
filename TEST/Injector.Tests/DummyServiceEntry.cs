/********************************************************************************
* DummyServiceEntry.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;

    public class DummyServiceEntry : AbstractServiceEntry
    {
        public DummyServiceEntry(Type iface, string name) : base(iface, name) { }

        public override AbstractServiceEntry CopyTo(IServiceRegistry owner)
        {
            throw new NotImplementedException();
        }

        public override bool SetInstance(IServiceReference serviceReference)
        {
            throw new NotImplementedException();
        }
    }
}
