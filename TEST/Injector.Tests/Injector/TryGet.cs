/********************************************************************************
* TryGet.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;

    public partial class InjectorTests
    {
        public interface INonExisting { }

        public static IEnumerable<List<(Type Interface, Type Implementation)>> BadRegistrations 
        {
            get 
            {
                yield return new List<(Type Interface, Type Implementation)>
                {
                    (typeof(IInterface_7<INonExisting>), typeof(Implementation_7_TInterface_Dependant<INonExisting>))
                };

                yield return new List<(Type Interface, Type Implementation)>
                {
                    (typeof(IInterface_7<INonExisting>), typeof(Implementation_7_TInterface_Dependant<INonExisting>)),
                    (typeof(IInterface_7<IInterface_7<INonExisting>>), typeof(Implementation_7_TInterface_Dependant<IInterface_7<INonExisting>>))
                };
            }
        }

        [Test]
        public void Injector_TryGet_ShouldReturnNullIfTheServiceNotFound()
        {
            Root = ScopeFactory.Create(svcs => { });

            using (IInjector injector = Root.CreateScope())
            {
                Assert.That(injector.TryGet<INonExisting>(), Is.Null);
            }
        }

        [TestCaseSource(nameof(BadRegistrations))]
        public void Injector_TryGet_ShouldThrowIfTheServiceCouldNotBeResolvedDueToAMissingDependency(List<(Type Interface, Type Implementation)> registrations)
        {
            Root = ScopeFactory.Create(svcs =>
            {
                foreach ((Type Interface, Type Implementation) reg in registrations)
                    if (reg.Implementation is not null)
                        svcs.Service(reg.Interface, reg.Implementation, Lifetime.Transient);
            });

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<ServiceNotFoundException>(() =>injector.TryGet(registrations.Last().Interface));
            }
        }

        [Test]
        public void Injector_TryGet_ShouldSupportNamedServices([Values(null, "cica")] string name, [ValueSource(nameof(Lifetimes))] Lifetime lifetime) 
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(name, lifetime));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.That(injector.TryGet<IInterface_1>(name), Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }
    }
}
