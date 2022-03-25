/********************************************************************************
* DummyServiceEntry.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class DummyServiceEntry : AbstractServiceEntry
    {
        public DummyServiceEntry(Type @interface, string? name) : base(@interface, name)
        {
        }

        public override AbstractServiceEntry WithOwner(IServiceRegistry owner) => throw new NotImplementedException();

        public override object CreateInstance(IInjector scope) => throw new NotImplementedException();

        public override object GetSingleInstance() => throw new NotImplementedException();
    }
}
