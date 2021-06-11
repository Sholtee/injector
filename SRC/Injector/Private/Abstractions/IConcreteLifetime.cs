/********************************************************************************
* IConcreteLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal interface IConcreteLifetime<TDescendant> where TDescendant: InjectorDotNetLifetime, IConcreteLifetime<TDescendant>, new() { }
}
