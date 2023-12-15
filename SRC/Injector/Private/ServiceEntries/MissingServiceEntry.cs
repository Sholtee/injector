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
        public MissingServiceEntry(Type type, object? key) : base(type, key, null, null, null, null)
        {
        }
    }
}
