/********************************************************************************
* SimpleDelegateCompiler.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;
    using Primitives.Patterns;

    internal sealed class SimpleDelegateCompiler : Singleton<SimpleDelegateCompiler>, IDelegateCompiler
    {
        public void Compile<TDelegate>(Expression<TDelegate> expression, Action<TDelegate> completionCallback) where TDelegate : Delegate
        {
            Debug.WriteLine($"Created compilation:{Environment.NewLine}{expression.GetDebugView()}");

            completionCallback
            (
                expression.Compile()
            );
        }
    }
}
