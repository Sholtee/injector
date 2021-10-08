/********************************************************************************
* CaptureDisposable.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Primitives.Patterns;

    internal sealed class CaptureDisposable: Disposable, ICaptureDisposable
    {
        //
        // - Azert Stack<> hogy forditott iranyban szabaditsuk fel a szervizeket mint ahogy letrehoztuk oket (igy az
        //   eppen felszabaditas alatt levo szerviz meg tudja hivatkozni a fuggosegeit).
        // - Ne Stack<IDisposable> legyen h tamogassuk azt a perverz esetet is ha egy szerviz csak az
        //   IAsyncDisposable-t valositja meg.
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

                        //
                        // Tamogassuk azt a pervezs esetet ha egy szerviz csak az IAsyncDisposable interface-t valositja meg.
                        //

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
            // Nem kell "base" hivas mert az a Dispose()-t hivja
            //
        }

        public void Capture(object obj)
        {
            //
            // Ellenorizzuk h az ujonan letrehozott peldanyt kesobb fel kell e szabaditani
            //

            if (obj is IDisposable || obj is IAsyncDisposable)
                FCapturedDisposables.Push(obj);
        }

        public IReadOnlyCollection<object> CapturedDisposables => FCapturedDisposables;
    }
}
