﻿/********************************************************************************
* IServiceReferenceExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives.Patterns;

    /// <summary>
    /// Defines some <see cref="IServiceReference"/> related extensions
    /// </summary>
    public static class IServiceReferenceExtensions
    {
        /// <summary>
        /// Gets the effective service instance from the given reference.
        /// </summary>
        public static object? GetInstance(this IServiceReference self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return self.Value is IWrapped<object> wrapped
                ? wrapped.Value
                : self.Value;
        }
    }
}