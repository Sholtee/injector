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

    internal sealed class ServiceCollection : HashSet<AbstractServiceEntry>, IModifiedServiceCollection
    {
        private AbstractServiceEntry? FLastEntry;

        //
        // TODO: FIXME: new ServiceCollection().Add() won't call this method...
        //

        bool ISet<AbstractServiceEntry>.Add(AbstractServiceEntry item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            bool success = Add(item);
            if (success)
                FLastEntry = item;
            
            return success;
        }

        public ServiceCollection(ServiceOptions? serviceOptions = null) : base(ServiceIdComparer.Instance)
            => ServiceOptions = serviceOptions ?? ServiceOptions.Default;

        public AbstractServiceEntry LastEntry => FLastEntry ?? throw new InvalidOperationException();

        public ServiceOptions ServiceOptions { get; }
    }
}