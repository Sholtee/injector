/********************************************************************************
* IInstanceFactory.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    internal interface IInstanceFactory<TDescendant> where TDescendant : IInstanceFactory<TDescendant>
    {
        TDescendant? Super { get; }

        object CreateInstance(AbstractServiceEntry requested);

        object Lock { get; }
    }

    internal interface IInstanceFactory<TDescendant, TSlot> : IInstanceFactory<TDescendant> where TDescendant : IInstanceFactory<TDescendant, TSlot>
    {
        ref TSlot? GetSlot(int slot);
    }
}
