/********************************************************************************
* TryGet.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using System.Collections;

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
        public void Injector_TryGet_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IInjectorBasicExtensions.TryGet<IDictionary>(null));
        }

        [Test]
        public void Injector_TryGet_ShouldThrowOnDisposedScope()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1>(Lifetime.Scoped));

            IInjector injector = Root.CreateScope();
            injector.Dispose();

            Assert.Throws<ObjectDisposedException>(() => injector.TryGet<IInterface_1>());
        }

        [Test]
        public void Injector_TryGet_ShouldReturnNullIfTheServiceNotFound([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => { }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                Assert.That(injector.TryGet<INonExisting>(), Is.Null);
            }
        }

        [Test]
        public void Injector_TryGet_ShouldThrowIfTheServiceCouldNotBeResolvedDueToAMissingDependency([ValueSource(nameof(BadRegistrations))] List<(Type Interface, Type Implementation)> registrations)
        {
            Root = ScopeFactory.Create
            (
                svcs =>
                {
                    foreach ((Type Interface, Type Implementation) reg in registrations)
                        if (reg.Implementation is not null)
                            svcs.Service(reg.Interface, reg.Implementation, Lifetime.Transient);
                },
                new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.JIT }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<ServiceNotFoundException>(() => injector.TryGet(registrations.Last().Interface));
            }
        }

        [Test]
        public void Injector_TryGet_ShouldSupportNamedServices([Values(null, "cica")] string name, [ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(name, lifetime), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                Assert.That(injector.TryGet<IInterface_1>(name), Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }
    }
}
