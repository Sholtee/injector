/********************************************************************************
* IDependencyResolver.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the contract how to resolve a dependency (<see cref="ParameterInfo"/> or <see cref="PropertyInfo"/>).
    /// </summary>
    public interface IDependencyResolver
    {
        /// <summary>
        /// Tries to resolve a dependency.
        /// </summary>
        /// <param name="injector">The <see cref="IInjector"/> instance to be used to resolve the dependency.</param>
        /// <param name="dependency">The dependency descriptor.</param>
        /// <param name="userData">Optional data, passed by the end-user.</param>
        /// <param name="next">Invokes the next resolver.</param>
        /// <returns>The resolution of the requiested dependency, for instance <code>(TDependency) injector.Get(dependency.Type, options.Name)</code></returns>
        Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, Next<Expression> next);
    }
}
