/********************************************************************************
* IReadOnlyServiceCollection.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the contract for a read-only <see cref="AbstractServiceEntry"/> collection.
    /// </summary>
    public interface IReadOnlyServiceCollection : IReadOnlyCollection<AbstractServiceEntry>
    {
    }
}