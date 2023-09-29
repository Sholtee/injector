/********************************************************************************
* IServiceCollection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the contract of service entry sets.
    /// </summary>
    /// <remarks>Only one entry can be registered with a particular <see cref="AbstractServiceEntry.Interface"/> and <see cref="AbstractServiceEntry.Name"/> pair.</remarks>
    public interface IServiceCollection : ICollection<AbstractServiceEntry>
    {
    }
}