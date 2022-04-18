/********************************************************************************
* CaptureDisposable.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Primitives.Patterns;

    internal sealed class CaptureDisposable: Disposable
    {
        //
        // - We use Stack<> to free instances in the inverse order as they were created (so the service being disposed
        //   still able to use its dependencies).
        // - Don't use Stack<IDisposable> to support the pervert scenario when a service implements the IAsyncDisposable
        //   interface only.
        //

        private readonly Stack<object> FCapturedDisposables = new(capacity: 5);

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                while (FCapturedDisposables.Count > 0)
                {
                    switch (FCapturedDisposables.Pop())
                    {
                        case IDisposable disposable:
                            disposable.Dispose();
                            break;
                        case IAsyncDisposable asyncDisposable:
                            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                            break;
                    }
                }
            }

            base.Dispose(disposeManaged);
        }

        protected override async ValueTask AsyncDispose()
        {
            while (FCapturedDisposables.Count > 0)
            {
                switch (FCapturedDisposables.Pop())
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync();
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }

            //
            // Don't call the base here since it would invoke the Dispose method().
            //
        }

        public void Capture(object obj)
        {
            //
            // The object must implement the IDisposable and/or the IAsyncDisposable interface
            //

            if (obj is IDisposable || obj is IAsyncDisposable)
                FCapturedDisposables.Push(obj);
        }

        public ICollection CapturedDisposables => FCapturedDisposables;
    }
}
