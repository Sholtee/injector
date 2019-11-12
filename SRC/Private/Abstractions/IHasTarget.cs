/********************************************************************************
* IHasTarget.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal interface IHasTarget<T>
    {
        T Target { get; }
    }
}
