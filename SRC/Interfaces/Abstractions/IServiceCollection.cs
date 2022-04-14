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
    /// <remarks><see cref="IServiceCollection"/> interface implements the <see cref="ISet{AbstractServiceEntry}"/> interface so you can register only one entry with the given <see cref="AbstractServiceEntry.Interface"/> and <see cref="AbstractServiceEntry.Name"/>.</remarks>
    public interface IServiceCollection : ISet<AbstractServiceEntry>
    {
    }
}