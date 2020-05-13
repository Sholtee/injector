/********************************************************************************
* IServiceEntry.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes a servie entry that can be stored in a <see cref="IServiceContainer"/>.
    /// </summary>
    public interface IServiceEntry : IServiceDefinition
    {
        /// <summary>
        /// The previously created service instance (if exists).
        /// </summary>
        object? Instance { get; }
    }
}
