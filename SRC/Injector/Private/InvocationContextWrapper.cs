/********************************************************************************
* InvocationContextWrapper.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Proxy;

    internal sealed class InvocationContextWrapper: IInvocationContext
    {
        private InvocationContextWrapper(InvocationContext original, IInterceptorAggregator parent, int index)
        {
            Parent = parent;
            Index = index;
            Original = original;
        }

        public InvocationContextWrapper(InvocationContext original, IInterceptorAggregator parent) : this(original, parent, 0)
        {
        }

        public object ProxyInstance => Parent;

        public IInterceptorAggregator Parent { get; }

        public InvocationContext Original { get; }

        public int Index { get; }

        public IInvocationContext Next => Index < Parent.Interceptors.Length
            ? new InvocationContextWrapper(Original, Parent, Index + 1)
            : throw new IndexOutOfRangeException();

        public object?[] Args => Original.Args;

        public MethodInfo InterfaceMethod => Original.InterfaceMethod;

        public MemberInfo InterfaceMember => Original.InterfaceMember;

        public MethodInfo TargetMethod => Original.TargetMethod;

        public MemberInfo TargetMember => Original.TargetMember;

        public object? InvokeInterceptor() => Index < Parent.Interceptors.Length
            ? Parent.Interceptors[Index].Invoke(this, static ctx => ctx.Next.InvokeInterceptor())
            : Parent.CallTarget(Original);
    }
}
