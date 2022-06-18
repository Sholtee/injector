/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Interfaces;

    /// <summary>
    /// Implements the <see cref="IServiceCollection"/> interface.
    /// </summary>
    public sealed class ServiceCollection : HashSet<AbstractServiceEntry>, IModifiedServiceCollection
    {
        private AbstractServiceEntry? FLastEntry;

        bool ISet<AbstractServiceEntry>.Add(AbstractServiceEntry item!!)
        {
            bool success = Add(item);
            if (success)
                FLastEntry = item;
            
            return success;
        }

        /// <summary>
        /// Creates a new <see cref="ServiceCollection"/> instance.
        /// </summary>
        public ServiceCollection() : base(ServiceIdComparer.Instance) { }

        /// <inheritdoc/>
        public AbstractServiceEntry LastEntry => FLastEntry ?? throw new InvalidOperationException();
    }
}