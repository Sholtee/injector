/********************************************************************************
* LazyHavingContext.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class LazyHavingContext<T, TContext> : ILazy<T>
    {
        private readonly object FLock = new();
        private readonly TContext FContext;
        private readonly Func<TContext, T> FFactory;
        private T? FValue;

        public LazyHavingContext(Func<TContext, T> factory, TContext context)
        {
            FFactory = factory;
            FContext = context;
        }

        public T Value
        {
            get
            {
                if (!IsValueCreated)
                    lock (FLock)
                        if (!IsValueCreated)
                        {
                            FValue = FFactory(FContext);
                            IsValueCreated = true;
                        }
                return FValue!;
            }
        }

        public bool IsValueCreated { get; private set; }
    }
}
