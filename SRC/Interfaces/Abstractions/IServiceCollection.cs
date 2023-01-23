/********************************************************************************
* IServiceCollection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the contract for an <see cref="AbstractServiceEntry"/> set.
    /// </summary>
    /// <remarks>Only one entry can be registered with a given <see cref="AbstractServiceEntry.Interface"/> and <see cref="AbstractServiceEntry.Name"/> pair.</remarks>
    public interface IServiceCollection : ICollection<AbstractServiceEntry>
    {
        /// <summary>
        /// Contains some constants related to the <see cref="IServiceCollection"/> interface.
        /// </summary>
        public static class Consts
        {
            /// <summary>
            /// Marks a service as internal
            /// </summary>
            [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Using upper case and underscore together highlights that the member is a constant value")]
            public const string INTERNAL_SERVICE_NAME_PREFIX = "$";
        }
    }
}