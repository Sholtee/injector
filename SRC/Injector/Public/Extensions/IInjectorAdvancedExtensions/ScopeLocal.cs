/********************************************************************************
* ScopeLocal.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    public static partial class IInjectorAdvancedExtensions
    {
        /// <summary>
        /// Assigns a scope-local variable. Scope-locals are instances (for example the HTTP request object) provided by the current session.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once (with the given <paramref name="key"/>).</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="value">The value  to be assigned. It cannot be null.</param>
        /// <remarks>The <paramref name="value"/> won't be disposed by the system.</remarks>
        public static void AssignScopeLocal(this IInjector self, Type type, object? key, object value)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            self.Get<ScopeLocal>(new
            {
                __type = type,
                __key = key
            }).Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Assigns a scope-local variable. Scope-locals are instances (for example the HTTP request object) provided by the current session.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once.</param>
        /// <param name="value">The value  to be assigned. It cannot be null.</param>
        /// <remarks>The <paramref name="value"/> won't be disposed by the system.</remarks>
        public static void AssignScopeLocal(this IInjector self, Type type, object value)
            => self.AssignScopeLocal(type, null, value);

        /// <summary>
        /// Assigns a scope-local variable. Scope-locals are instances (for example the HTTP request object) provided by the current session.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="value">The value  to be assigned. It cannot be null.</param>
        /// <remarks>The <paramref name="value"/> won't be disposed by the system.</remarks>
        public static void AssignScopeLocal<TType>(this IInjector self, object? key, TType value)
            => self.AssignScopeLocal(typeof(TType), key, value!);

        /// <summary>
        /// Assigns a scope-local variable. Scope-locals are instances (for example the HTTP request object) provided by the current session.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="value">The value  to be assigned. It cannot be null.</param>
        /// <remarks>The <paramref name="value"/> won't be disposed by the system.</remarks>
        public static void AssignScopeLocal<TType>(this IInjector self, TType value)
            => self.AssignScopeLocal(type: typeof(TType), value!);
    }
}
