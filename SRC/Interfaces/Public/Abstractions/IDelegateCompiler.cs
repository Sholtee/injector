/********************************************************************************
* IDelegateCompiler.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the contract of delegate compilers.
    /// </summary>
    public interface IDelegateCompiler
    {
        /// <summary>
        /// Compiles the given <paramref name="expression"/>.
        /// </summary>
        void Compile<TDelegate>(Expression<TDelegate> expression, Action<TDelegate> completionCallback) where TDelegate: Delegate;
    }
}
