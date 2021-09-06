/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ServiceCollection : HashSet<AbstractServiceEntry>, IModifiedServiceCollection
    {
        private AbstractServiceEntry? FLastEntry;

        bool ISet<AbstractServiceEntry>.Add(AbstractServiceEntry item)
        {
            bool success = Add(item);
            if (success)
                FLastEntry = item;
            return success;
        }

        public ServiceCollection() : base(ServiceIdComparer.Instance) { }

        public AbstractServiceEntry LastEntry => FLastEntry ?? throw new InvalidOperationException();
    }
}