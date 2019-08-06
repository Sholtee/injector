/********************************************************************************
* InjectorEventHandler.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI
{
    /// <summary>
    /// Represent the method that handles an event raised by the <see cref="IInjector"/>.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="arg">The argument of the event.</param>
    public delegate void InjectorEventHandler<in TEventArg>(IInjector sender, TEventArg arg) where TEventArg: InjectorEventArg;
}
