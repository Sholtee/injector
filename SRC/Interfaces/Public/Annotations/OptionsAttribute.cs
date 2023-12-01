/********************************************************************************
* OptionsAttribute.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Controls the <see cref="IInjector"/> during the dependency resolution.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>
    /// It's safe to omit this annotation. The default behaviour is:
    /// <code>scope.Get(typeof(TParameterType|TPropertyType), null)</code>
    /// </item>
    /// <item>
    /// You can annotate parameters and properties as well.
    /// </item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class OptionsAttribute: Attribute
    {
        /// <summary>
        /// The (optional) service key. The value of this property will be passed to the corresponding scope invocation:
        /// <code>
        /// class MyService
        /// {
        ///     public MyService([Options(Key = "...")] IDependency dependency) {...}
        /// }
        /// </code>
        /// translates to
        /// <code>scope => new MyService(scope.Get(typeof(IDependency), options.Key))</code>
        /// </summary>
        public object? Key { get; init; }

        /// <summary>
        /// Indicates whether a dependency is optional or not. In practice:
        /// <code>
        /// class MyService
        /// {
        ///     public MyService([Options(Optional = true)] IDependency dependency) {...}
        /// }
        /// </code>
        /// translates to
        /// <code>scope => new MyService(scope.TryGet(typeof(IDependency), options.Key))</code>
        /// while
        /// <code>
        /// class MyService
        /// {
        ///     public MyService([Options(Optional = false)] IDependency dependency) {...}
        /// }
        /// </code>
        /// becomes
        /// <code>scope => new MyService(scope.Get(typeof(IDependency), options.Key))</code>
        /// </summary>
        /// <remarks>This option is ignored if you are using the MS preferred DI (<see cref="IServiceProvider"/>).</remarks>
        public bool Optional { get; init; }
    }
}
