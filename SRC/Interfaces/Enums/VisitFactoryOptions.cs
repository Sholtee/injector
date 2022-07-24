/********************************************************************************
* VisitFactoryOptions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines some options for the <see cref="AbstractServiceEntry.VisitFactory(Func{LambdaExpression, LambdaExpression}, VisitFactoryOptions)"/> method.
    /// </summary>
    [Flags]
    [SuppressMessage("Design", "CA1008:Enums should have zero value")]
    public enum VisitFactoryOptions : int
    {
        /// <summary>
        /// The default state.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Instructs the system to build the factory delegate after visiting the chain.
        /// </summary>
        BuildDelegate = 1 << 0,
    }
}
