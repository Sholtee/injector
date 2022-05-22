/********************************************************************************
* AspectAttribute.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines an abstract aspect that can be applied against service interfaces.
    /// </summary>
    /// <remarks>Derived aspects must implement the <see cref="IInterceptorFactory{TInterceptor}"/> interface.</remarks>
    [AttributeUsage(AttributeTargets.Interface)]
    public abstract class AspectAttribute : Attribute
    {
    }
}
