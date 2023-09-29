/********************************************************************************
* DummyServiceEntry.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class DummyServiceEntry : MissingServiceEntry
    {
        public DummyServiceEntry(Type @interface, object? name) : base(@interface, name)
        {
        }
    }
}
