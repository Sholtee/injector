/********************************************************************************
* ServiceOptions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the general service behavior.
    /// </summary>
    public record ServiceOptions
    {
        /// <summary>
        /// If set to true, the system tries to apply proxies defined via <see cref="AspectAttribute"/> class.
        /// </summary>
        /// <remarks>The default value for this property is <code>true</code></remarks>
        public bool SupportAspects { get; init; } = true;

        /// <summary>
        /// The proxy engine to be used. Leave blank to use the default engine.
        /// </summary>
        public IProxyEngine? ProxyEngine { get; init; }

        /// <summary>
        /// The default options.
        /// </summary>
        public static ServiceOptions Default { get; } = new();
    }
}
