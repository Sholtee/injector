/********************************************************************************
* IFactoryVisitor.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines the contract of visiting factory methods.
    /// </summary>
    public interface IFactoryVisitor
    {
        /// <summary>
        /// Represents a visit step.
        /// </summary>
        LambdaExpression Visit(LambdaExpression factory, AbstractServiceEntry entry);
    }
}