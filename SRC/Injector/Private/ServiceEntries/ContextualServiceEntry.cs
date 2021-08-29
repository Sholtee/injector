/********************************************************************************
* ContextualServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ContextualServiceEntry : AbstractServiceEntry
    {
        public ContextualServiceEntry(Type @interface, Func<IInjector, Type, object> factory) : base(@interface, null, null!)
        {
            Factory = factory;
        }


    }
}