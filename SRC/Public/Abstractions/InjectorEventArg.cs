/********************************************************************************
* InjectornEventArg.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.DI
{
    using Properties;

    /// <summary>
    /// Contains the <see cref="IInjector"/> event data.
    /// </summary>
    public class InjectorEventArg
    {
        private object FService;

        /// <summary>
        /// Creates a new <see cref="InjectorEventArg"/> instance.
        /// </summary>
        /// <param name="interface">The service interface.</param>
        /// <param name="target">The (optional) target who requested the service.</param>
        public InjectorEventArg(Type @interface, Type target)
        {
            Interface = @interface;
            Target = target;
        }

        /// <summary>
        /// Creates a new <see cref="InjectorEventArg"/> instance.
        /// </summary>
        /// <param name="target">The target who requested the service.</param>
        /// <remarks>This constructor is tipically used in <see cref="IInjectorExtensions.Instantiate"/> calls.</remarks>
        public InjectorEventArg(Type target): this(null, target)
        {           
        }

        /// <summary>
        /// The service interface (if it is present).
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The (optional) target who invocated the <see cref="IInjector"/>.
        /// </summary>
        public Type Target { get; }

        /// <summary>
        /// The newly created service (if the invocation was successful). 
        /// </summary>
        public object Service
        {
            get => FService;
            set => FService = Interface == null || Interface.IsInstanceOfType(value)
                ? value
                : throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, Interface, value.GetType()));
        }
    }
}
