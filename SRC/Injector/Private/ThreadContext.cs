/********************************************************************************
* ThreadContext.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    internal sealed class ThreadContext
    {
        public IReadOnlyList<Type> CurrentPath { get; set; }
    }
}
