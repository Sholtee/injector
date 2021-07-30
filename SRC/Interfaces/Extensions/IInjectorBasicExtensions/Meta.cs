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
        [SuppressMessage("Performance", "CA1802:Use literals where appropriate", Justification = "Due to interpolation it cannot be const")]
        private static readonly string META_NAME = $"{Consts.INTERNAL_SERVICE_NAME_PREFIX}meta";

        /// <summary>
        /// Sets a meta-data on the given <see cref="IInjector"/> (scope).
        /// </summary>
        public static void Meta(this IInjector self, string key, object? value)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            IDictionary<string, object?>? metaContainer = self.TryGet<IDictionary<string, object?>>(META_NAME);
            if (metaContainer is null)
            {
                metaContainer = new Dictionary<string, object?>();
                self.UnderlyingContainer.Instance(META_NAME, metaContainer);
            }

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

            object? val = null;
            self.TryGet<IDictionary<string, object?>>(META_NAME)?.TryGetValue(key, out val);
            
            return val;
        }
    }
}