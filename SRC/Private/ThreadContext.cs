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
        public IReadOnlyList<Type> CurrentPath { get; set; }
    }
}
