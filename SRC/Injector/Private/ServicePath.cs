/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

    internal sealed class ServicePath: IServicePath
    {
        private readonly List<AbstractServiceEntry> FPath = new(capacity: 10);

        public void CheckNotCircular()
        {
            if (FPath.Count <= 1)
                return;

            AbstractServiceEntry last = FPath[^1];

            int firstIndex = 0;
            bool found = false;

            for(; firstIndex < FPath.Count; firstIndex++)
            {
                AbstractServiceEntry current = FPath[firstIndex];

                if (current.Interface == last.Interface && current.Name == last.Name)
                {
                    found = true;
                    break;
                }
            }

            Debug.Assert(found);

            //
            // Ha egynel tobbszor szerepel az aktualis szerviz az aktualis utvonalon akkor korkoros referenciank van.
            //

            if (firstIndex < FPath.Count - 1) throw new CircularReferenceException
            (
                string.Format(Resources.Culture, Resources.CIRCULAR_REFERENCE, Format(GetCircle()))
            );

            //
            // Csak magat a kort adjuk vissza.
            //

            IEnumerable<AbstractServiceEntry> GetCircle()
            {
                for (int i = firstIndex; i < FPath.Count; i++)
                {
                    yield return FPath[i];
                }
            }
        }

        public void Push(AbstractServiceEntry entry) => FPath.Add(entry);

        public void Pop() =>
            //
            // Utolso elem eltavolitasa gyors muvelet [nincs Array.Copy()]:
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

        public IEnumerator<AbstractServiceEntry> GetEnumerator() => FPath.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
