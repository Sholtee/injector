/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Text;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    //                                        !!!FIGYELEM!!!
    //
    // Ez az osztaly kozponti komponens, ezert minden modositast korultekintoen, a teljesitmenyt szem elott tartva
    // kell elvegezni:
    // - nincs Sysmte.Linq
    // - nincs System.Reflection
    // - mindig futtassuk a teljesitmeny teszteket (is) hogy a hatekonysag nem romlott e
    //

    internal sealed class ServicePath
    {
        private readonly List<AbstractServiceEntry> FPath = new(capacity: 10);

        public void Push(AbstractServiceEntry entry)
        {
            int foundIndex = 0;

            for(; foundIndex < FPath.Count; foundIndex++)
            {
                AbstractServiceEntry that = FPath[foundIndex];

                //
                // In most of cases ReferenceEquals() is fine, the only exception is when we compare
                // specialized entries [entry.Specialize(MyType) != entry.Specialize(MyType)]
                //

                if (ReferenceEquals(that, entry) || (that.Interface == entry.Interface && that.Name == entry.Name))
                    break;
            }

            //
            // If the service is included more than once in the current route, it indicates that there is a 
            // circular reference in the graph.
            //

            if (foundIndex < FPath.Count) throw new CircularReferenceException
            (
                string.Format(Resources.Culture, Resources.CIRCULAR_REFERENCE, Format(GetCircle()))
            );

            FPath.Add(entry);

            //
            // Return the circle itself.
            //

            IEnumerable<AbstractServiceEntry> GetCircle()
            {
                for (int i = foundIndex; i < FPath.Count; i++)
                {
                    yield return FPath[i];
                }

                yield return entry;
            }
        }

        public void Pop() =>
            //
            // Removing the last entry is a quick operation [there is no Array.Copy()]:
            // https://github.com/dotnet/runtime/blob/78593b9e095f974305b2033b465455e458e30267/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs#L925
            //

            FPath.RemoveAt(FPath.Count - 1);

        public int Count => FPath.Count;

        public AbstractServiceEntry this[int index] => FPath[index];

        public static string Format(IEnumerable<AbstractServiceEntry> path)
        {
            StringBuilder sb = new();

            foreach (AbstractServiceEntry entry in path)
            {
                if (sb.Length > 0)
                    sb.Append(" -> ");

                sb.Append(entry.ToString(shortForm: true));
            }

            return sb.ToString();
        }
    }
}
