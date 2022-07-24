/********************************************************************************
* ServiceEntryFeatures.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

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
        /// Calling the <see cref="AbstractServiceEntry.VisitFactory(Func{LambdaExpression, LambdaExpression}, VisitFactoryOptions)"/> method is supportedif the entry has a factory function defined.
        /// </summary>
        SupportsVisit = 1 << 2
    }
}
