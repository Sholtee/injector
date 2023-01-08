/********************************************************************************
* ServiceEntryFeatures.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the features of an <see cref="AbstractServiceEntry"/>
    /// </summary>
    [Flags]
    public enum ServiceEntryFeatures: int
    {
        /// <summary>
        /// The default state.
        /// </summary>
        Default = 0,

        /// <summary>
        /// A single instance will be created.
        /// </summary>
        CreateSingleInstance = 1 << 0,

        /// <summary>
        /// The created service instance is shared between scopes.
        /// </summary>
        Shared = 1 << 1,

        /// <summary>
        /// Calling the <see cref="AbstractServiceEntry.Build(IBuildContext?, IFactoryVisitor[])"/> method is supported.
        /// </summary>
        SupportsBuild = 1 << 2,

        /// <summary>
        /// Proxying is supported via <see cref="AspectAttribute"/> class.
        /// </summary>
        SupportsAspects = 1 << 3

        //
        // TBD: SupportsProxying
        //
    }
}
