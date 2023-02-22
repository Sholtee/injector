/********************************************************************************
* IAspect.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines the contract of aspects.
    /// </summary>
    public interface IAspect
    {
        /// <summary>
        /// Gets the concrete factory responsible for creating the interceptor instance.
        /// </summary>
        Expression<CreateInterceptorDelegate> GetFactory(AbstractServiceEntry relatedEntry);
    }
}