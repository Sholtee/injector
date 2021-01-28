/********************************************************************************
* Composite.Config.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Primitives.Patterns;

    public partial class Config 
    {
        /// <summary>
        /// Defines several options to control the behavior of the <see cref="IComposite{T}"/> objects.
        /// </summary>
        public class ServiceContainerConfig
        {
            /// <summary>
            /// Limits the count of children belong to a <see cref="IComposite{T}"/> entity.
            /// </summary>
            public int MaxChildCount { get; set; } = GetValue<int?>("ServiceContainer.MaxChildCount") ?? 512;
        }

        /// <summary>
        /// <see cref="ServiceContainerConfig"/>.
        /// </summary>
        public ServiceContainerConfig ServiceContainer { get; } = new ServiceContainerConfig();
    }
}
