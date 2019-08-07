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

        public InjectorEventArg(Type @interface, Type target)
        {
            Interface = @interface;
            Target = target;
        }

        public InjectorEventArg(Type target): this(null, target)
        {           
        }

        /// <summary>
        /// The service interface (if presents).
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The (optional) target who invocated the <see cref="IInjector"/>.
        /// </summary>
        public Type Target { get; }

        /// <summary>
        /// The newly created service (if the invocation was successfull). 
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
