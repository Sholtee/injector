/********************************************************************************
* InjectorEventArg.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
#if NETSTANDARD1_6
using System.Reflection;
#endif

namespace Solti.Utils.DI
{
    using Properties;
    using Internals;

    /// <summary>
    /// Contains the <see cref="IInjector"/> event data.
    /// </summary>
    public class InjectorEventArg: IServiceID
    {
        private object FService;

        /// <summary>
        /// Creates a new <see cref="InjectorEventArg"/> instance.
        /// </summary>
        /// <param name="interface">The service interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="target">The (optional) target who requested the service.</param>
        public InjectorEventArg(Type @interface, string name, Type target)
        {
            Interface = @interface;
            Name = name;
            Target = target;
        }

        /// <summary>
        /// The service interface (if it is present).
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The (optional) name of the service.
        /// </summary>
        public string Name { get; }

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
                : throw new Exception(string.Format(Resources.INVALID_INSTANCE, Interface));
        }
    }
}
