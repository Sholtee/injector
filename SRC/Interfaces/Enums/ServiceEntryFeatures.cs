/********************************************************************************
* ServiceEntryFeatures.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the features of an <see cref="AbstractServiceEntry"/>
    /// </summary>
    [Flags]
    [SuppressMessage("Design", "CA1008:Enums should have zero value")]
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
        /// Calling the <see cref="AbstractServiceEntry.Build(IDelegateCompiler?, Func{int}, IFactoryVisitor[])"/> method is supported.
        /// </summary>
        SupportsBuild = 1 << 2

        //
        // TBD: SupportsProxying
        //
    }
}
