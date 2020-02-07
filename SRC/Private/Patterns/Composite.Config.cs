/********************************************************************************
* Composite.Config.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Defines several options to control the behavior of the <see cref="IComposite{T}"/> objects.
    /// </summary>
    public class CompositeConfig
    {
        /// <summary>
        /// Limits the count of children belong to a <see cref="IComposite{T}"/> entity.
        /// </summary>
        public int MaxChildCount { get; set; } = 512;
    }

    public partial class Config 
    {
        /// <summary>
        /// <see cref="InjectorConfig"/>.
        /// </summary>
        public CompositeConfig Composite { get; set; } = new CompositeConfig();
    }
}
