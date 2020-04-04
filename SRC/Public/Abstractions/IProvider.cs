/********************************************************************************
* IProvider.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Describes the mechanism of providing services.
    /// </summary>
    public interface IProvider 
    {
        /// <summary>
        /// Creates a new service instance.
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "The identifier won't confuse the users of the API.")]
        object Get();
    }
}