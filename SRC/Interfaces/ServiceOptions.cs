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
    public class ServiceOptions
    {
        /// <summary>
        /// If set to true, the system tries to apply proxies defined via <see cref="AspectAttribute"/> class.
        /// </summary>
        /// <remarks>The default value for this property is <code>true</code></remarks>
        public bool SupportAspects { get; init; } = true;

        /// <summary>
        /// The default options.
        /// </summary>
        public static ServiceOptions Default { get; } = new();
    }
}
