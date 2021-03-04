/********************************************************************************
* ServiceContainer.Config.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    public partial class Config 
    {
        /// <summary>
        /// Defines some options that control the behavior of the <see cref="DI.ServiceContainer"/> objects.
        /// </summary>
        public class ServiceContainerConfig
        {
            /// <summary>
            /// Limits the count of the nested containers.
            /// </summary>
            public int MaxChildCount { get; set; } = GetValue<int?>("ServiceContainer.MaxChildCount") ?? 512;
        }

        /// <summary>
        /// <see cref="ServiceContainerConfig"/>.
        /// </summary>
        public ServiceContainerConfig ServiceContainer { get; } = new ServiceContainerConfig();
    }
}
