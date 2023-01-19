/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    //
    // Don't expose this class directly as it lacks some List<AbstractServiceEntry> overrides
    //

    internal sealed class ServiceCollection : List<AbstractServiceEntry>, IServiceCollection
    {
        public new void Add(AbstractServiceEntry item)
        {
            if (this.Contains(item ?? throw new ArgumentNullException(nameof(item)), ServiceIdComparer.Instance))
                throw new ServiceAlreadyRegisteredException(Resources.SERVICE_ALREADY_REGISTERED);
  
            base.Add(item);
        }

        public ServiceCollection() { }

        public ServiceCollection(IServiceCollection src)
        {
            foreach (AbstractServiceEntry entry in src)
            {
                Add(entry);
            }
        }
    }
}