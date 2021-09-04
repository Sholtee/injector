/********************************************************************************
* Meta.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    public static partial class IInjectorBasicExtensions
    {
        /// <summary>
        /// Name of the "meta" entry
        /// </summary>
        [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
        public static readonly string META_NAME = $"{Consts.INTERNAL_SERVICE_NAME_PREFIX}meta";

        /// <summary>
        /// Sets a meta-data on the given <see cref="IInjector"/> (scope).
        /// </summary>
        public static void Meta(this IInjector self, string key, object? value)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            IDictionary<string, object?>? metaContainer = self.Get<IDictionary<string, object?>>(META_NAME);
            metaContainer[key] = value;
        }

        /// <summary>
        /// Gets a meta-data associated with the given <see cref="IInjector"/> (scope).
        /// </summary>
        public static object? Meta(this IInjector self, string key)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            self
                .Get<IDictionary<string, object?>>(META_NAME)
                .TryGetValue(key, out object? val);
            
            return val;
        }
    }
}