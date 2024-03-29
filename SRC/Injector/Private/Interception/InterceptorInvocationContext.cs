﻿/********************************************************************************
* InterceptorInvocationContext.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Proxy;

    internal class InterceptorInvocationContext: IInvocationContext
    {
        protected InterceptorInvocationContext(InvocationContext original, IInterceptorAggregator parent)
        {
            Original = original;
            Parent = parent;
        }

        public InterceptorInvocationContext(InvocationContext original, IInterceptorAggregator parent, int index): this(original, parent)
        {
            Index = index;
        }

        public object ProxyInstance => Parent;

        public IInterceptorAggregator Parent { get; }

        public InvocationContext Original { get; }

        public int Index { get; }

        public object?[] Args => Original.Args;

        public MethodInfo InterfaceMethod => Original.InterfaceMethod;

        public MemberInfo InterfaceMember => Original.InterfaceMember;

        public MethodInfo TargetMethod => Original.TargetMethod;

        public MemberInfo TargetMember => Original.TargetMember;

        public object? UserData { get; set; }

        public virtual IInvocationContext? Next => IInvocationContextFactory.Create(Original, Parent, Index + 1);

        public virtual object? InvokeInterceptor() => Parent.Interceptors[Index].Invoke
        (
            this,
            static ctx => ctx.Next!.InvokeInterceptor()
        );
    }
}
