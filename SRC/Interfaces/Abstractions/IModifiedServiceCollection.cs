/********************************************************************************
* IModifiedServiceCollection.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Extends the <see cref="IServiceCollection"/> interface with a property of <see cref="LastEntry"/>.
    /// </summary>
    public interface IModifiedServiceCollection : IServiceCollection
    {
        /// <summary>
        /// The last added service.
        /// </summary>
        AbstractServiceEntry LastEntry { get; }
    }
}