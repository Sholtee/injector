/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal sealed class ServicePath: IServicePath
    {
        private readonly LinkedList<IServiceReference> FPath = new();

        public IServiceReference? Last => FPath.Last?.Value;

        public IServiceReference? First => FPath.First?.Value;

        public void CheckNotCircular()
        {
            Ensure.IsNotNull(Last, nameof(Last));

            AbstractServiceEntry last = Last!.RelatedServiceEntry;

            int firstIndex = 0;
            bool found = false;

            foreach (IServiceReference reference in FPath)
            {
                AbstractServiceEntry current = reference.RelatedServiceEntry;

                if (current.Interface == last.Interface && current.Name == last.Name)
                {
                    found = true;
                    break;
                }

                firstIndex++;
            }

            Debug.Assert(found);

            //
            // Ha egynel tobbszor szerepel az aktualis szerviz az aktualis utvonalon akkor korkoros referenciank van.
            //

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
                        FPath.Skip(firstIndex)
                    )
                ));
        }

        public void Push(IServiceReference node)
        {
            Ensure.Parameter.IsNotNull(node, nameof(node));

            FPath.AddLast(node);
        }

        public void Pop() => FPath.RemoveLast();

        public static string Format(IEnumerable<IServiceReference> path) => Format(path.Select(svc => svc.RelatedServiceEntry));

        public static string Format(IEnumerable<IServiceId> path) => string.Join(" -> ", path.Select(IServiceIdExtensions.FriendlyName));

        public IEnumerator<IServiceReference> GetEnumerator() => FPath.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
