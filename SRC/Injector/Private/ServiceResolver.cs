﻿/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
#if DEBUG // test methods must be public -> make internal types public so they can be passed as parameter
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public
#else
    internal
#endif 
    sealed record ServiceResolver(AbstractServiceEntry RelatedEntry, Func<IInstanceFactory, object> Resolve);
}