﻿/********************************************************************************
* IBuildContext.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives;

    /// <summary>
    /// Context used during the build phase.
    /// </summary>
    public interface IBuildContext
    {
        /// <summary>
        /// Creates a new slot and return its ID
        /// </summary>
        /// <remarks>Calling this method is required to store scoped service instances.</remarks>
        int AssignSlot();

        /// <summary>
        /// The compiler to be used to build <see cref="LambdaExpression"/>s.
        /// </summary>
        DelegateCompiler Compiler { get; }
    }
}
