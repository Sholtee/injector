/********************************************************************************
* LazyTypeResolver.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("System.Runtime.Loader.AssemblyLoadContext_Solti.Utils.DI.IAssemblyLoadContext_Duck")]

namespace Solti.Utils.DI
{
    using Utils.Proxy;
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
    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Child types should not call interface methods")]
    public class LazyTypeResolver: ITypeResolver
    {
        private Assembly? FAssembly;

        private readonly string FAssemblyPath;

        internal IAssemblyLoadContext AssemblyLoadContext { get; }

        internal LazyTypeResolver(Type iface, string asmPath, string className, IAssemblyLoadContext assemblyLoadContext) // tesztekhez
        {
            Ensure.Parameter.IsInterface(iface, nameof(iface));

            FAssemblyPath = Path.GetFullPath(asmPath);

            Interface = iface;
            ClassName = className;
            AssemblyLoadContext = assemblyLoadContext;
        }

        /// <summary>
        /// Creates a new <see cref="LazyTypeResolver"/> instance.
        /// </summary>
        /// <param name="iface">The service interface.</param>
        /// <param name="asmPath">The absolute path of the containing <see cref="System.Reflection.Assembly"/>. This assembly will be loaded (using <see cref="System.Runtime.Loader.AssemblyLoadContext"/>) on the first <see cref="ITypeResolver.Resolve"/> call.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <paramref name="iface"/>.</param>
        public LazyTypeResolver(Type iface, string asmPath, string className): this(
            Ensure.Parameter.IsNotNull(iface, nameof(iface)), 
            Ensure.Parameter.IsNotNull(asmPath, nameof(asmPath)), 
            Ensure.Parameter.IsNotNull(className, nameof(className)), 
            System.Runtime.Loader.AssemblyLoadContext.Default.Act().Like<IAssemblyLoadContext>())
        {
        }

        /// <summary>
        /// The full name of the <see cref="Type"/> that implemenets the <see cref="Interface"/>.
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        /// The service interface.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The lazily loaded <see cref="System.Reflection.Assembly"/>. Reading this property for the first time will load the assembly on path specified in the constructor.
        /// </summary>
        public Assembly Assembly
        {
            get
            {
                if (FAssembly == null)
                    FAssembly = AssemblyLoadContext.LoadFromAssemblyPath(FAssemblyPath);
                return FAssembly;
            }
        }
     
        bool ITypeResolver.Supports(Type iface)
        {
            Ensure.Parameter.IsNotNull(iface, nameof(iface));

            return iface == Interface;
        }

        Type ITypeResolver.Resolve(Type iface)
        {
            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Type.Supports(this, iface);
  
            Type result = Assembly.GetType(ClassName, throwOnError: true);
            Ensure.Type.Supports(result, iface);

            return result;
        }
    }

    /// <summary>
    /// Implements a resolver that postpones the <see cref="Assembly"/> and <see cref="Type"/> resolution until the first request.
    /// </summary>
    public class LazyTypeResolver<TInterface> : LazyTypeResolver where TInterface : class
    {
        /// <summary>
        /// Creates a new <see cref="LazyTypeResolver{TInterface}"/> instance.
        /// </summary>
        /// <param name="asmPath">The absolute path of the containing <see cref="Assembly"/>. This assembly will be loaded (using <see cref="System.Runtime.Loader.AssemblyLoadContext"/>) on the first <see cref="ITypeResolver.Resolve"/> call.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <typeparamref name="TInterface"/>.</param>
        public LazyTypeResolver(string asmPath, string className) : base(typeof(TInterface), asmPath, className)
        {
        }
    }
}
