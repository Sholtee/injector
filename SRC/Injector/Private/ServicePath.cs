/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Properties;

    internal sealed class ServicePath: IServicePath
    {
        private readonly Stack<IServiceReference> FPath = new();

        public IServiceReference? Requestor => FPath.Any() ? FPath.Peek() : null;

        public void CheckNotCircular()
        {
            //
            // Ha egynel tobbszor szerepel az aktualis szerviz az aktualis utvonalon akkor korkoros referenciank van.
            //

            int firstIndex = this.FirstIndexOf(Ensure.IsNotNull(Requestor, nameof(Requestor)), ServiceReferenceComparer.Instance);

            if (firstIndex < FPath.Count - 1)
                throw new CircularReferenceException(string.Format
                (
                    Resources.Culture,
                    Resources.CIRCULAR_REFERENCE,

                    //
                    // Csak magat a kort adjuk vissza.
                    //

                    Format
                    (
                        this.Skip(firstIndex)
                    )
                ));
        }

        public IDisposable With(IServiceReference node) 
        {
            FPath.Push(node);
            return new WithScope(FPath);
        }

        private sealed class WithScope : Disposable 
        {
            private readonly Stack<IServiceReference> FPath;

            public WithScope(Stack<IServiceReference> path) => FPath = path;

            protected override void Dispose(bool disposeManaged)
            {
                FPath.Pop();
                base.Dispose(disposeManaged);
            }
        }

        public static string Format(IEnumerable<IServiceReference> path) => Format(path.Select(svc => svc.RelatedServiceEntry));

        public static string Format(IEnumerable<IServiceId> path) => string.Join(" -> ", path.Select(IServiceIdExtensions.FriendlyName));

        public IEnumerator<IServiceReference> GetEnumerator() =>
            //
            // Verem elemek felsorolasa forditva tortenik -> Reverse()
            //

            FPath.Reverse().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
