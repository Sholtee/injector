/********************************************************************************
* IServiceCollection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the contract for an <see cref="AbstractServiceEntry"/> set.
    /// </summary>
    public interface IServiceCollection : ISet<AbstractServiceEntry>
    {
    }
}