/********************************************************************************
* ServiceEnumerator.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    //                                        !!!FIGYELEM!!!
    //
    // Ez az osztaly kozponti komponens, ezert minden modositast korultekintoen, a teljesitmenyt szem elott tartva
    // kell elvegezni:
    // - nincs Sysmte.Linq
    // - nincs System.Reflection
    // - mindig futtassuk a teljesitmeny teszteket (is) hogy a hatekonysag nem romlott e
    //

    internal sealed class ServiceEnumerator<TInterface>: IEnumerable<TInterface> where TInterface: class
    {
        public ServiceEnumerator(IInjector injector) => Injector = injector;

        public IInjector Injector { get; }

        public IEnumerator<TInterface> GetEnumerator()
        {
            Func<AbstractServiceEntry, bool> isCompatible = !typeof(TInterface).IsGenericType
                ? entry => entry.Interface == typeof(TInterface)

                //
                // a) IList<int> volt regisztralva
                // b) IList<> volt regisztralva
                //

                : entry => entry.Interface == typeof(TInterface) || entry.Interface == typeof(TInterface).GetGenericTypeDefinition();

            //
            // Lezart generikus mellett szerepelhet annak nyitott parja is -> Distinct
            //

            HashSet<string?> hashSet = new();

            foreach (AbstractServiceEntry entry in Injector.Get<IServiceRegistry>().RegisteredEntries)
            {
                if (isCompatible(entry) && hashSet.Add(entry.Name))
                {
                    yield return Injector.Get<TInterface>(entry.Name);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
