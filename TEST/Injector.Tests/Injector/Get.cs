/********************************************************************************
* Get.cs                                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;
    using Properties;

    using ScopeFactory = DI.ScopeFactory;

    public partial class InjectorTests
    {
        [Test]
        public void Injector_Get_ShouldThrowOnNonInterfaceKey()
        {
            Root = ScopeFactory.Create(svcs => { });

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<ArgumentException>(() => injector.Get<Object>(), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
                Assert.Throws<ArgumentException>(() => injector.Get(typeof(Object)), string.Format(Resources.PARAMETER_NOT_AN_INTERFACE, "iface"));
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Get_ShouldInstantiate(Lifetime lifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(lifetime));

            using (IInjector injector = Root.CreateScope())
            {
                var instance = injector.Get<IInterface_1>();

                Assert.That(instance, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Get_ShouldInstantiateEnumerables(Lifetime lifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(lifetime));

            using (IInjector injector = Root.CreateScope())
            {
                var enumerable = injector.Get<IEnumerable<IInterface_1>>();

                Assert.That(enumerable.Count(), Is.EqualTo(1));
                Assert.That(enumerable.Single(), Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnNonRegisteredDependency([ValueSource(nameof(Lifetimes))] Lifetime lifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime));

            using (IInjector injector = Root.CreateScope())
            {
                var e = Assert.Throws<ServiceNotFoundException>(() => injector.Get<IInterface_7<IInterface_1>>());
                Assert.That(e.Data.Contains("path"));
                Assert.That(e.Data["path"], Is.EqualTo(string.Join(" -> ", new ServiceId(typeof(IInterface_7<IInterface_1>), null).FriendlyName(), new ServiceId(typeof(IInterface_1), null).FriendlyName())));
            }
        }

        [Test]
        public void Injector_Get_ShouldNotThrowOnNonRegisteredDependencyInCaseOfEnumerables()
        {
            Root = ScopeFactory.Create(svcs => { });

            using (IInjector injector = Root.CreateScope())
            {
                var enumerable = injector.Get<IEnumerable<IInterface_1>>();

                Assert.That(enumerable.Any(), Is.False);
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(lifetime1)
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime2)); // direkt masodikkent szerepel

            using (IInjector injector = Root.CreateScope())
            {
                var instance = injector.Get<IInterface_2>();

                Assert.That(instance, Is.InstanceOf<Implementation_2_IInterface_1_Dependant>());
                Assert.That(instance.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveLazyDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime1)
                .Service<IInterface_2_LazyDep, Implementation_2_Lazy__IInterface_1_Dependant>(lifetime2));

            using (IInjector injector = Root.CreateScope())
            {
                var instance = injector.Get<IInterface_2_LazyDep>();

                Assert.That(instance, Is.InstanceOf<Implementation_2_Lazy__IInterface_1_Dependant>());
                Assert.That(instance.Interface1, Is.InstanceOf<Lazy<IInterface_1>>());
                Assert.That(instance.Interface1.Value, Is.InstanceOf<IInterface_1>());
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Get_ShouldResolveGenericDependencies(Lifetime lifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                .Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), Lifetime.Transient)
                .Service(typeof(IInterface_6<>), typeof(Implementation_6_IInterface_3_Dependant<>), lifetime));

            using (IInjector injector = Root.CreateScope())
            {         
                var instance = injector.Get<IInterface_6<string>>();
            
                Assert.That(instance, Is.InstanceOf<Implementation_6_IInterface_3_Dependant<string>>());
                Assert.That(instance.Interface3, Is.InstanceOf<Implementation_3_IInterface_1_Dependant<string>>());
                Assert.That(instance.Interface3.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Get_ShouldThrowOnOpenGenericType(Lifetime lifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), lifetime));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<ArgumentException>(() => injector.Get(typeof(IInterface_3<>)), Resources.PARAMETER_IS_GENERIC);
            }          
        }

        [Test]
        public void Injector_Get_ShouldThrowOnNull()
        {
            Root = ScopeFactory.Create(svcs => { });

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<ArgumentNullException>(() => injector.Get(null));
            }
        }

        private interface IMyService 
        {
            void DoSomething();
        }

        private class ImplementationHavingInlineDep : IMyService
        {
            public IInjector Injector { get; }

            public ImplementationHavingInlineDep(IInjector injector) => Injector = injector;

            public void DoSomething() => Injector.Get<IInterface_1>();
        }

        [Test]
        public void Injector_Get_ShouldWorkInline([ValueSource(nameof(Lifetimes))] Lifetime requestorLifetime, [ValueSource(nameof(Lifetimes))] Lifetime depLifetime) 
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(depLifetime)
                .Service<IMyService, ImplementationHavingInlineDep>(requestorLifetime)
                .Service<IInterface_7<IMyService>, Implementation_7_TInterface_Dependant<IMyService>>(Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                IMyService svc = injector.Get<IInterface_7<IMyService>>().Interface;

                Assert.DoesNotThrow(svc.DoSomething);
            }
        }

        [Test]
        public void Injector_GetByProvider_ShouldThrowOnCircularReference()
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_4, Implementation_4_CDep>(Lifetime.Transient)
                .Service<IInterface_5, Implementation_5_CDep>(Lifetime.Transient)
                .Provider<IInterface_1, CdepProvider>(Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
            }
        }

        private sealed class CdepProvider : IServiceProvider 
        {
            public CdepProvider(IInterface_4 dep) { }
            public object GetService(Type serviceType) => throw new NotImplementedException();
        }

        [Test]
        public void Injector_GetByService_ShouldThrowOnCircularReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_4, Implementation_4_CDep>(lifetime1)
                .Service<IInterface_5, Implementation_5_CDep>(lifetime2));

            using (IInjector injector = Root.CreateScope())
            {     
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_GetByFactory_ShouldThrowOnCircularReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Factory<IInterface_4>(injector => new Implementation_4_CDep(injector.Get<IInterface_5>()), lifetime1)
                .Factory<IInterface_5>(injector => new Implementation_5_CDep(injector.Get<IInterface_4>()), lifetime2));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_GetByCtor_ShouldThrowOnCircularReference()
        {
            //
            // IInjector.Get() hivasok a konstruktorban vannak
            //

            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_7_CDep>(Lifetime.Transient)
                .Service<IInterface_4, Implementation_4_CDep>(Lifetime.Transient)
                .Service<IInterface_5, Implementation_5_CDep>(Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_GetByProxy_ShouldThrowOnCircularReference(Lifetime lifetime)
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(lifetime).WithProxy((injector, _, _) => injector.Get<IInterface_1>()));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_1)));
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnRecursiveReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime) 
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_10_RecursiveCDep>(lifetime));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_1)));
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveItself()
        {
            Root = ScopeFactory.Create(svcs => { });

            using (IInjector injector = Root.CreateScope())
            {
                Assert.AreSame(injector, injector.Get<IInjector>());
            }         
        }

        [Test]
        public void Injector_Get_ShouldResolveItselfAsACtorParameter()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInjector>, Implementation_7_TInterface_Dependant<IInjector>>(Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                var svc = injector.Get<IInterface_7<IInjector>>();

                Assert.That(svc.Interface, Is.EqualTo(injector));
            }
        }

        [Test]
        public void Injector_Get_ShouldBeTypeChecked() 
        {
            Root = ScopeFactory.Create(svcs => svcs.Factory(typeof(IInterface_1), (injector, iface) => new object(), Lifetime.Transient));

            using (IInjector injector = Root.CreateScope()) 
            {
                Assert.Throws<InvalidCastException>(() => injector.Get<IInterface_1>(), string.Format(Resources.INVALID_INSTANCE, typeof(IInterface_1)));
                Assert.That(injector.Get<IServiceRegistry>().GetEntry<IInterface_1>().Instances, Is.Empty);
            }
        }

        [Test]
        public void Injector_Get_ShouldDisposeTheServiceReferenceOnError()
        {
            HackyServiceEntry entry = new();
            Root = ScopeFactory.Create(svcs => svcs.Register(entry));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<Exception>(() => injector.Get<IInterface_1>());

                Assert.That(entry.GotReference.RefCount == 0);
            }
        }

        private sealed class HackyServiceEntry : AbstractServiceEntry
        {
            public IServiceReference GotReference { get; private set; }

            public HackyServiceEntry() : base(typeof(IInterface_1), null) { }

            public override AbstractServiceEntry CopyTo(IServiceRegistry owner) => this;

            public override bool SetInstance(IServiceReference serviceReference)
            {
                GotReference = serviceReference;
                throw new Exception();
            }
        }

        [Test]
        public void Injector_Get_ShouldNotThrowIfAMissingDependencyIsOptional()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInterface_1>, Implementation_7_UsingOptionalDependency>(Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_7<IInterface_1> svc = null;

                Assert.DoesNotThrow(() => svc = injector.Get<IInterface_7<IInterface_1>>());
                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.Null);
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveOptionalDependencies()
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                .Service<IInterface_7<IInterface_1>, Implementation_7_UsingOptionalDependency>(Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.That(injector.Get<IInterface_7<IInterface_1>>().Interface, Is.Not.Null);
            }
        }

        private sealed class Implementation_7_UsingOptionalDependency : Implementation_7_TInterface_Dependant<IInterface_1>
        {
            public Implementation_7_UsingOptionalDependency([Options(Optional = true)] IInterface_1 dep) : base(dep)
            {
            }
        }

        [Test]
        public void Injector_Get_ShouldNotThrowIfAMissingLazyDependencyIsOptional()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<Lazy<IInterface_1>>, Implementation_7_UsingOptionalLazyDependency>(Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_7<Lazy<IInterface_1>> svc = null;

                Assert.DoesNotThrow(() => svc = injector.Get<IInterface_7<Lazy<IInterface_1>>>());
                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Interface, Is.Not.Null);
                Assert.That(svc.Interface.Value, Is.Null);
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveOptionalLazyDependencies()
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                .Service<IInterface_7<Lazy<IInterface_1>>, Implementation_7_UsingOptionalLazyDependency>(Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.That(injector.Get<IInterface_7<Lazy<IInterface_1>>>().Interface.Value, Is.Not.Null);
            }
        }

        private sealed class Implementation_7_UsingOptionalLazyDependency : Implementation_7_TInterface_Dependant<Lazy<IInterface_1>>
        {
            public Implementation_7_UsingOptionalLazyDependency([Options(Optional = true)] Lazy<IInterface_1> dep) : base(dep)
            {
            }
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Get_ShouldThrowIfTheServiceIsNotProducible(Lifetime lifetime) 
        {
            Root = ScopeFactory.Create(svcs =>
            {
                var setter = (ISupportsProxying) svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(lifetime)
                    .LastEntry;

                setter.Factory = null;
            });

            using (IInjector injector = Root.CreateScope()) 
            {
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_1>(), Resources.NOT_PRODUCIBLE);
            }
        }

        [Test]
        public void Injector_Get_ShouldNotSpecializeIfTheClosedPairOfTheOpenGenericServiceWasRegistered()
        {
            Root = ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                .Service(typeof(IInterface_3<>), typeof(NotUsedImplementation<>), Lifetime.Transient)
                .Service<IInterface_3<int>, Implementation_3_IInterface_1_Dependant<int>>(Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_3<int> svc = injector.Get<IInterface_3<int>>();

                Assert.That(svc, Is.InstanceOf<Implementation_3_IInterface_1_Dependant<int>>());
            }
        }

        private sealed class NotUsedImplementation<T> : IInterface_3<T>
        {
            public IInterface_1 Interface1 => throw new NotImplementedException();
        }

        public class MyServiceUsingItsOnwerInjectorOnDisposal : Disposable, IMyService
        {
            public IInjector Injector { get; }

            public MyServiceUsingItsOnwerInjectorOnDisposal(IInjector injector)
            {
                Injector = injector;
            }

            public void DoSomething()
            {
            }

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    Injector.Get<IScopeFactory>();

                base.Dispose(disposeManaged);
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowIfItIsCalledInsideDispose()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IMyService, MyServiceUsingItsOnwerInjectorOnDisposal>(Lifetime.Scoped));

            IInjector injector = Root.CreateScope();
            injector.Get<IMyService>();
            Assert.Throws<InvalidOperationException>(injector.Dispose, Resources.INJECTOR_IS_BEING_DISPOSED);
        }

        [Test]
        public void Injector_Get_ShouldExtractWrappedService()
        {
            IDisposable obj = new Disposable();

            Mock<IWrapped<object>> mockWrapped = new(MockBehavior.Strict);
            mockWrapped
                .SetupGet(x => x.Value)
                .Returns(obj);
            mockWrapped
                .Setup(x => x.Dispose());

            Root = ScopeFactory.Create(svcs => svcs.Factory(typeof(IDisposable), (i, t) => mockWrapped.Object, Lifetime.Transient));

            using (IInjector injector = Root.CreateScope())
            {
                Assert.DoesNotThrow(() => injector.Get<IDisposable>());
            }
        }
    }
}
