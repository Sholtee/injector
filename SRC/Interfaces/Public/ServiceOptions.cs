/********************************************************************************
* ServiceOptions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the general service behavior.
    /// </summary>
    public sealed record ServiceOptions
    {
        /// <summary>
        /// If set to true, the system tries to apply proxies defined via <see cref="AspectAttribute"/> class.
        /// </summary>
        /// <remarks>
        /// You may install interceptors directly by using the decorator recipe:
        /// <code>
        /// coll.Service&lt;IMyService, MyService&gt;().Decorate((injector, type, instance) => ...);
        /// </code>
        /// </remarks>
        public bool SupportAspects { get; set; } = true;

        /// <summary>
        /// The proxy engine to be used. Leave blank to use the default engine.
        /// </summary>
        /// <remarks>The default value for this property is provided by the <a href="https://github.com/Sholtee/proxygen">ProxyGen.NET</a> library.</remarks>
        public IProxyEngine? ProxyEngine { get; set; }

        /// <summary>
        /// Contains the descriptor how to dispose created service instances.
        /// </summary>
        public ServiceDisposalMode DisposalMode { get; set; } = ServiceDisposalMode.Force;

        /// <summary>
        /// User defined dependency resolvers. Leave blank to use the default ones.
        /// </summary>
        /// <remarks>
        /// A <see cref="IDependencyResolver"/> specifies how to resolve a particular dependency type (for instance a lazy dependency).
        /// Default resolvers (in order) are:
        /// <list type="bullet">
        /// <item>
        /// <i>explicit argument resolver</i>: <code>public MyServiceConstructor(NonInterfaceDependency dep) {...}</code> 
        /// </item>
        /// <item>
        /// <i>lazy dependency resolver</i>: <code>public MyServiceConstructor(Lazy&lt;IRegularDependency&gt; dep) {...}</code> 
        /// </item>
        /// <item>
        /// <i>regular dependency resolver</i>: <code>public MyServiceConstructor(IRegularDependency dep) {...}</code> 
        /// </item>
        /// </list>
        /// </remarks>
        public IReadOnlyList<IDependencyResolver>? DependencyResolvers { get; set; }

        /// <summary>
        /// The default options.
        /// </summary>
        /// <remarks>
        /// Modifying the <see cref="Default"/> value will impact the whole system. Therefore it's recommended to use the <i>with</i> pattern to set options when registering a concrete service:
        /// <code>
        /// coll.Service&lt;IMyService, MyService&gt;(Lifetime.Singleton, ServiceOptions.Default with { DisposalMode = ... });
        /// </code>
        /// </remarks>
        public static ServiceOptions Default { get; } = new();
    }
}
