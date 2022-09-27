/********************************************************************************
* SimpleDelegateCompiler.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Private.Expressions.DelegateCompilers
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed class SimpleDelegateCompiler : Singleton<SimpleDelegateCompiler>, IDelegateCompiler
    {
        public void Compile<TDelegate>(Expression<TDelegate> expression, Action<TDelegate> completionCallback) => completionCallback(expression.Compile());
    }
}
