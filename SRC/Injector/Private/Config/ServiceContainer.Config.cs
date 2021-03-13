/********************************************************************************
* ServiceContainer.Config.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Defines some options that control the behavior of the <see cref="DI.ServiceContainer"/> objects.
    /// </summary>
    public class ServiceContainerConfig
    {
        /// <summary>
        /// Limits the count of the nested containers.
        /// </summary>
        public int MaxChildCount { get; set; } = Config.GetValue<int?>("ServiceContainer.MaxChildCount") ?? 512;
    }

    public partial class Config
    {
        /// <summary>
        /// <see cref="ServiceContainerConfig"/>.
        /// </summary>
        public ServiceContainerConfig ServiceContainer { get; } = new ServiceContainerConfig();
    }
}
