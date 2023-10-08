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
    using Properties;

    //
    // Don't expose this class directly as it lacks some HashSet<AbstractServiceEntry> overrides
    //

    internal sealed class ServiceCollection : HashSet<AbstractServiceEntry>, IServiceCollection
    {
        public new void Add(AbstractServiceEntry item)
        {
            if (!base.Add(item ?? throw new ArgumentNullException(nameof(item))))
                throw new ServiceAlreadyRegisteredException(Resources.SERVICE_ALREADY_REGISTERED, item);
        }

        public ServiceCollection(): base(ServiceIdComparer.Instance) { }

        public ServiceCollection(IServiceCollection src): this()
        {
            foreach (AbstractServiceEntry entry in src)
            {
                Add(entry);
            }
        }
    }
}