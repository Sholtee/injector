/********************************************************************************
* LazyTypeResolver.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("System.Runtime.Loader.AssemblyLoadContext_Solti.Utils.DI.IAssemblyLoadContext_Duck")]

namespace Solti.Utils.DI
{
    using Proxy;
    using Properties;
    using Internals;

    //
    // Az AssemblyLoadContext-nek nincs absztrakcioja valamint a LoadFromAssemblyPath() sem
    // virtualis -> korulmenyes lenne kimockolni duck typing nelkul.
    //

    internal interface IAssemblyLoadContext
    {
        Assembly LoadFromAssemblyPath(string assemblyPath);
    }

    /// <summary>
    /// Implements a resolver that postpones the <see cref="System.Reflection.Assembly"/> and <see cref="Type"/> resolution until the first request.
    /// </summary>
    /// <typeparam name="TInterface">The target interface.</typeparam>
    public class LazyTypeResolver<TInterface>: ITypeResolver where TInterface: class
    {
        private readonly object FLock = new object();
    
        private Assembly FAssembly;

        internal IAssemblyLoadContext AssemblyLoadContext { get; }

        internal LazyTypeResolver(string asmPath, string className, IAssemblyLoadContext assemblyLoadContext) // tesztekhez
        {
            if (!typeof(TInterface).IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE);

            AssamblyPath = asmPath;
            ClassName = className;
            AssemblyLoadContext = assemblyLoadContext;
        }

        /// <summary>
        /// Creates a new <see cref="LazyTypeResolver{TInterface}"/> instance.
        /// </summary>
        /// <param name="asmPath">The absolute path of the containing <see cref="System.Reflection.Assembly"/>. This assembly will be loaded (using <see cref="System.Runtime.Loader.AssemblyLoadContext"/>) on the first <see cref="ITypeResolver.Resolve"/> call.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <typeparamref name="TInterface"/>.</param>
        public LazyTypeResolver(string asmPath, string className): this(asmPath, className, System.Runtime.Loader.AssemblyLoadContext.Default.Act().Like<IAssemblyLoadContext>())
        {
        }

        /// <summary>
        /// The absolute path of the containing <see cref="System.Reflection.Assembly"/>.
        /// </summary>
        public string AssamblyPath { get; }

        /// <summary>
        /// The full name of the <see cref="Type"/> that implemenets the <typeparamref name="TInterface"/>.
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        /// The lazily loaded <see cref="System.Reflection.Assembly"/>.
        /// </summary>
        public Assembly Assembly
        {
            get
            {
                if (FAssembly == null)
                    lock (FLock)
                        if (FAssembly == null)
                            FAssembly = AssemblyLoadContext.LoadFromAssemblyPath(AssamblyPath);
                return FAssembly;
            }
        }

        /// <summary>
        /// See <see cref="ITypeResolver.Supports"/>.
        /// </summary>
        public virtual bool Supports(Type @interface) => @interface == typeof(TInterface);

        /// <summary>
        /// See <see cref="ITypeResolver.Resolve"/>.
        /// </summary>
        public virtual Type Resolve(Type @interface)
        {
            if (Supports(@interface))
            {
                Type result = Assembly.GetType(ClassName, throwOnError: true);

                if (@interface.IsAssignableFrom(result))
                    return result;
            }
            throw new NotSupportedException(string.Format(Resources.INTERFACE_NOT_SUPPORTED, @interface));
        }
    }
}
