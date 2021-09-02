/********************************************************************************
* ServiceEnumerator.cs                                                          *
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

    internal sealed class ServiceEnumerator<TInterface>: IEnumerable<TInterface> where TInterface: class
    {
        public ServiceEnumerator(IInjector injector) => Injector = injector;

        public IInjector Injector { get; }

        public IEnumerator<TInterface> GetEnumerator()
        {
            foreach (string? svcName in GetNames())
            {
                yield return Injector.Get<TInterface>(svcName);
            }

            IEnumerable<string?> GetNames() 
            {
                Func<AbstractServiceEntry, bool> filter = !typeof(TInterface).IsGenericType
                    ? entry => entry.Interface == typeof(TInterface)
                    : entry => entry.Interface == typeof(TInterface) || entry.Interface == typeof(TInterface).GetGenericTypeDefinition();

                return Injector
                    .Get<IServiceRegistry>()
                    .RegisteredEntries
                    .Where(filter)
                    .Select(entry => entry.Name)

                    //
                    // Lezart generikus mellett szerepelhet annak nyitott parja is
                    //

                    .Distinct();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
