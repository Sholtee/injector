/********************************************************************************
* MissingServiceEntry.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class MissingServiceEntry : AbstractServiceEntry
    {
        public MissingServiceEntry(Type @interface, object? name) : base(@interface, name, null, null, null, null)
        {
        }
    }
}
