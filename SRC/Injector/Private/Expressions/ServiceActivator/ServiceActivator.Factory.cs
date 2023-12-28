/********************************************************************************
* ServiceActivator.Factory.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal partial class ServiceActivator
    {
        public Expression<FactoryDelegate> ResolveFactory(ConstructorInfo constructor, object? userData) =>
            CreateActivator<FactoryDelegate>(constructor, userData);
    }
}