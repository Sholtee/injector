/********************************************************************************
* ThreadContext.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal sealed class ThreadContext
    {
        public Stack<Type> CurrentPath { get; }

        public ThreadContext() => CurrentPath = new Stack<Type>();
    }
}
