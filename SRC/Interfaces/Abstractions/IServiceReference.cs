﻿/********************************************************************************
* IServiceReference.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Encapsulates a service and its dependencies into a reference counted container.
    /// </summary>
    public interface IServiceReference
    {
        /// <summary>
        /// The related service entry.
        /// </summary>
        AbstractServiceEntry RelatedServiceEntry { get; }

        /// <summary>
        /// The (optional) <see cref="IInjector"/> instance who created this reference.
        /// </summary>
        IInjector? RelatedInjector { get; }

        /// <summary>
        /// The dependencies of the bound service.
        /// </summary>
        IReadOnlyCollection<IServiceReference> Dependencies { get; }

        /// <summary>
        /// Registers a new dependency in the <see cref="Dependencies"/> list.
        /// </summary>
        void AddDependency(IServiceReference dependency);

        /// <summary>
        /// The bound service instance. The value can be set once.
        /// </summary>
        object? Value { get; set; }

        /// <summary>
        /// Increments the reference counter of this instance.
        /// </summary>
        /// <returns></returns>
        int AddRef();

        /// <summary>
        /// Decrements the reference counter of this instance and disposes the <see cref="Value"/> if it reaches the zero.
        /// </summary>
        int Release();

        /// <summary>
        /// Decrements the reference counter of this instance and disposes the <see cref="Value"/> asynchronously if it reaches the zero.
        /// </summary>
        Task<int> ReleaseAsync();
#if DEBUG
        /// <summary>
        /// The current reference count of this object.
        /// </summary>
        int RefCount { get; }
#endif
    }
}
