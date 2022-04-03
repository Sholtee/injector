/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
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

    internal sealed class ServicePath: IServicePath
    {
        private readonly List<AbstractServiceEntry> FPath = new(capacity: 10);

        public void Push(AbstractServiceEntry entry)
        {
            int foundIndex = 0;

            for(; foundIndex < FPath.Count; foundIndex++)
            {
                AbstractServiceEntry current = FPath[foundIndex];

                //
                // TODO: After removing the AbstractServiceEntry.CopyTo() method, a reference comparison
                //       will be enough here.
                //       

                if (current.Interface == entry.Interface && current.Name == entry.Name)
                    break;
            }

            //
            // Ha egynel tobbszor szerepelne az aktualis szerviz az aktualis utvonalon akkor korkoros referenciank van.
            //

            if (foundIndex < FPath.Count) throw new CircularReferenceException
            (
                string.Format(Resources.Culture, Resources.CIRCULAR_REFERENCE, Format(GetCircle()))
            );

            FPath.Add(entry);

            //
            // Csak magat a kort adjuk vissza.
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
