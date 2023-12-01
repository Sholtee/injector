/********************************************************************************
* Get.cs                                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;
    using Properties;

    public partial class InjectorTests
    {
        [Test]
        public void Injector_Get_ShouldThrowOnDisposedScope()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1>(Lifetime.Scoped));
            
            IInjector injector = Root.CreateScope();
            injector.Dispose();

            Assert.Throws<ObjectDisposedException>(() => injector.Get<IInterface_1>());
        }

        [Test]
        public void Injector_Get_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IInjectorBasicExtensions.Get<IDictionary>(null));
        }

        [Test]
        public void Injector_Get_ShouldHaveTimeout()
        {
            ManualResetEventSlim evt = new();   
            Func<IInjector, Type, object> fact = Factory;

            Root = ScopeFactory.Create(svcs => svcs.Factory(typeof(IList<>), factoryExpr: (i, t) => fact(i, t), Lifetime.Singleton), ScopeOptions.Default with { ResolutionLockTimeout = TimeSpan.Zero });

            bool t1Started = false;
            Task t1 = Task.Factory.StartNew(() => { t1Started = true; return Root.CreateScope().Get<IList<int>>(); });
            SpinWait.SpinUntil(() => t1Started);

            bool t2Started = false;
            Task t2 = Task.Factory.StartNew(() => { t2Started = true; return Root.CreateScope().Get<IList<int>>(); });
            SpinWait.SpinUntil(() => t2Started);

            Thread.Sleep(10);
            evt.Set();

            Assert.DoesNotThrow(t1.Wait);
            Assert.Throws<TimeoutException>(() => t2.GetAwaiter().GetResult());

            object Factory(IInjector injector, Type iface)
            {
                evt.Wait();
                return Activator.CreateInstance(typeof(List<>).MakeGenericType(iface.GetGenericArguments()));
            }
        }

        [Test]
        public void Injector_Get_ShouldInstantiate_Iface([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(lifetime), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_1 instance = injector.Get<IInterface_1>();

                Assert.That(instance, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Get_ShouldInstantiate_Class([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<Implementation_1_No_Dep, Implementation_1_No_Dep>(lifetime), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                Implementation_1_No_Dep instance = injector.Get<Implementation_1_No_Dep>();

                Assert.That(instance, Is.Not.Null);
            }
        }

        [Test]
        public void Injector_Get_ShouldInstantiate_GenericIface([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service(typeof(IMyGenericService<>), typeof(MyGenericService<>), lifetime), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                IMyGenericService<int> instance = injector.Get<IMyGenericService<int>>();

                Assert.That(instance, Is.InstanceOf<MyGenericService<int>>());
            }
        }

        [Test]
        public void Injector_Get_ShouldInstantiate_GenericClass([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service(typeof(MyGenericService<>), typeof(MyGenericService<>), lifetime), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                MyGenericService<int> instance = injector.Get<MyGenericService<int>>();

                Assert.That(instance, Is.Not.Null);
            }
        }

        [Test]
        public void Injector_Get_ShouldInstantiateEnumerables([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1_No_Dep>(lifetime), new ScopeOptions { ServiceResolutionMode = resolutionMode });

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
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime), new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.JIT });

            using (IInjector injector = Root.CreateScope())
            {
                var ex = Assert.Throws<ServiceNotFoundException>(() => injector.Get<IInterface_7<IInterface_1>>());

                Assert.That(ex.Requested, Is.EqualTo(new ServiceId(typeof(IInterface_1), null)).Using(IServiceId.Comparer.Instance));
                // Assert.That(ex.Requestor, Is.EqualTo(new DummyServiceEntry(typeof(IInterface_7<IInterface_1>), null)).Using(ServiceIdComparer.Instance));
            }
        }

        [Test]
        public void Injector_Get_ShouldNotThrowOnNonRegisteredDependencyInCaseOfEnumerables([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => { }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                var enumerable = injector.Get<IEnumerable<IInterface_1>>();

                Assert.That(enumerable.Any(), Is.False);
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveInterfaceDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(lifetime1)
                    .Service<IInterface_1, Implementation_1_No_Dep>(lifetime2), // direkt masodikkent szerepel
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            ); 

            using (IInjector injector = Root.CreateScope())
            {
                var instance = injector.Get<IInterface_2>();

                Assert.That(instance, Is.InstanceOf<Implementation_2_IInterface_1_Dependant>());
                Assert.That(instance.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveClassDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<Implementation_1_No_Dep, Implementation_1_No_Dep>(lifetime1)
                    .Service<IInterface_7<Implementation_1_No_Dep>, Implementation_7<Implementation_1_No_Dep>>(lifetime2),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_7<Implementation_1_No_Dep> instance = injector.Get<IInterface_7<Implementation_1_No_Dep>>();

                Assert.That(instance, Is.Not.Null);
                Assert.That(instance.Dependency, Is.Not.Null);
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveLazyInterfaceDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(lifetime1)
                    .Service<IInterface_2_LazyDep, Implementation_2_Lazy__IInterface_1_Dependant>(lifetime2),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

            using (IInjector injector = Root.CreateScope())
            {
                var instance = injector.Get<IInterface_2_LazyDep>();

                Assert.That(instance, Is.InstanceOf<Implementation_2_Lazy__IInterface_1_Dependant>());
                Assert.That(instance.Interface1, Is.InstanceOf<Lazy<IInterface_1>>());
                Assert.That(instance.Interface1.Value, Is.InstanceOf<IInterface_1>());
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveLazyClassDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<Implementation_1_No_Dep, Implementation_1_No_Dep>(lifetime1)
                    .Service<IInterface_7<Lazy<Implementation_1_No_Dep>>, Implementation_7<Lazy<Implementation_1_No_Dep>>> (lifetime2),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_7<Lazy<Implementation_1_No_Dep>> instance = injector.Get<IInterface_7<Lazy<Implementation_1_No_Dep>>>();

                Assert.That(instance, Is.InstanceOf<IInterface_7<Lazy<Implementation_1_No_Dep>>>());
                Assert.That(instance.Dependency, Is.InstanceOf<Lazy<Implementation_1_No_Dep>>());
                Assert.That(instance.Dependency.Value, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveGenericInterfaceDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                    .Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), Lifetime.Transient)
                    .Service(typeof(IInterface_6<>), typeof(Implementation_6_IInterface_3_Dependant<>), lifetime),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

            using (IInjector injector = Root.CreateScope())
            {         
                var instance = injector.Get<IInterface_6<string>>();
            
                Assert.That(instance, Is.InstanceOf<Implementation_6_IInterface_3_Dependant<string>>());
                Assert.That(instance.Interface3, Is.InstanceOf<Implementation_3_IInterface_1_Dependant<string>>());
                Assert.That(instance.Interface3.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveGenericClassDependencies([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                    .Service(typeof(Implementation_3_IInterface_1_Dependant<>), typeof(Implementation_3_IInterface_1_Dependant<>), Lifetime.Transient)
                    .Service<IInterface_7<Implementation_3_IInterface_1_Dependant<int>>, Implementation_7<Implementation_3_IInterface_1_Dependant<int>>>(lifetime),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_7<Implementation_3_IInterface_1_Dependant<int>> instance = injector.Get<IInterface_7<Implementation_3_IInterface_1_Dependant<int>>>();

                Assert.That(instance, Is.InstanceOf<Implementation_7<Implementation_3_IInterface_1_Dependant<int>>>());
                Assert.That(instance.Dependency, Is.Not.Null);
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnOpenGenericType([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service(typeof(IInterface_3<>), typeof(Implementation_3_IInterface_1_Dependant<>), lifetime), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<ArgumentException>(() => injector.Get(typeof(IInterface_3<>)), Resources.PARAMETER_IS_GENERIC);
            }          
        }

        [Test]
        public void Injector_Get_ShouldThrowOnNull([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => { }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

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
        public void Injector_Get_ShouldWorkInline([ValueSource(nameof(Lifetimes))] Lifetime requestorLifetime, [ValueSource(nameof(Lifetimes))] Lifetime depLifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode) 
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(depLifetime)
                    .Service<IMyService, ImplementationHavingInlineDep>(requestorLifetime)
                    .Service<IInterface_7<IMyService>, Implementation_7_TInterface_Dependant<IMyService>>(Lifetime.Transient),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

            using (IInjector injector = Root.CreateScope())
            {
                IMyService svc = injector.Get<IInterface_7<IMyService>>().Dependency;

                Assert.DoesNotThrow(svc.DoSomething);
            }
        }

        [Test]
        public void Injector_GetByProvider_ShouldThrowOnCircularReference()
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_4, Implementation_4_CDep>(Lifetime.Transient)
                    .Service<IInterface_5, Implementation_5_CDep>(Lifetime.Transient)
                    .Provider<IInterface_1, CdepProvider>(Lifetime.Transient),
                new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.JIT }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
            }
        }

        [Test]
        public void Injector_GetByProvider_ShouldThrowIfTheProviderReturnsNull([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs.Provider<IInterface_1, ServiceProviderReturningNull>(lifetime),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_1>(), Resources.IS_NULL);
            }
        }

        private sealed class ServiceProviderReturningNull : IServiceProvider
        {
            public object GetService(Type serviceType) => null;
        }

        private sealed class CdepProvider : IServiceProvider 
        {
            public CdepProvider(IInterface_4 dep) { }
            public object GetService(Type serviceType) => throw new NotImplementedException();
        }

        [Test]
        public void Injector_GetByService_ShouldThrowOnCircularReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_4, Implementation_4_CDep>(lifetime1)
                    .Service<IInterface_5, Implementation_5_CDep>(lifetime2),
                new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.JIT }
            );

            using (IInjector injector = Root.CreateScope())
            {     
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_GetByFactory_ShouldThrowOnCircularReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime1, [ValueSource(nameof(Lifetimes))] Lifetime lifetime2)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Factory<IInterface_4>(factoryExpr: injector => new Implementation_4_CDep(injector.Get<IInterface_5>(null)), lifetime1)
                    .Factory<IInterface_5>(factoryExpr: injector => new Implementation_5_CDep(injector.Get<IInterface_4>(null)), lifetime2),
                new ScopeOptions {ServiceResolutionMode = ServiceResolutionMode.JIT }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
            }
        }

        [Test]
        public void Injector_GetByFactory_ShouldThrowIfTheFactoryReturnsNull([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [Values(ServiceResolutionMode.JIT, ServiceResolutionMode.AOT)] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs.Factory<IInterface_1>(factoryExpr: injector => (IInterface_1) null, lifetime),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_1>(), Resources.IS_NULL);
            }
        }

        [Test]
        public void Injector_GetByCtor_ShouldThrowOnCircularReference()
        {
            //
            // IInjector.Get() hivasok a konstruktorban vannak
            //

            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_7_CDep>(Lifetime.Transient)
                    .Service<IInterface_4, Implementation_4_CDep>(Lifetime.Transient)
                    .Service<IInterface_5, Implementation_5_CDep>(Lifetime.Transient),
                new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.JIT }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
            }
        }

        [Test]
        public void Injector_GetByDecorator_ShouldThrowOnCircularReference([ValueSource(nameof(Lifetimes))]Lifetime lifetime)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(lifetime).Decorate((injector, _, _) => injector.Get<IInterface_1>(null)),
                new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.JIT }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_1)));
            }
        }

        [Test]
        public void Injector_Get_ShouldThrowOnRecursiveReference([ValueSource(nameof(Lifetimes))] Lifetime lifetime) 
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_10_RecursiveCDep>(lifetime), new ScopeOptions { ServiceResolutionMode = ServiceResolutionMode.JIT });

            using (IInjector injector = Root.CreateScope())
            {
                Assert.Throws<CircularReferenceException>(() => injector.Get<IInterface_1>(), string.Join(" -> ", typeof(IInterface_1), typeof(IInterface_1)));
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveItself([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => { }, new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                Assert.AreSame(injector, injector.Get<IInjector>());
            }         
        }

        [Test]
        public void Injector_Get_ShouldResolveItselfAsACtorParameter([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInjector>, Implementation_7_TInterface_Dependant<IInjector>>(Lifetime.Transient), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                var svc = injector.Get<IInterface_7<IInjector>>();

                Assert.That(svc.Dependency, Is.EqualTo(injector));
            }
        }

        [Test]
        public void Injector_Get_ShouldBeTypeChecked([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode) 
        {
            Root = ScopeFactory.Create(svcs => svcs.Factory(typeof(IInterface_1), factoryExpr: (injector, iface) => new object(), lifetime), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope()) 
            {
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_1>());
            }
        }

        [Test]
        public void Injector_Get_ShouldNotThrowIfAMissingDependencyIsOptional([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInterface_1>, Implementation_7_UsingOptionalDependency>(Lifetime.Transient), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_7<IInterface_1> svc = null;

                Assert.DoesNotThrow(() => svc = injector.Get<IInterface_7<IInterface_1>>());
                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Dependency, Is.Null);
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveOptionalDependencies([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                    .Service<IInterface_7<IInterface_1>, Implementation_7_UsingOptionalDependency>(Lifetime.Transient),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.That(injector.Get<IInterface_7<IInterface_1>>().Dependency, Is.Not.Null);
            }
        }

        private sealed class Implementation_7_UsingOptionalDependency : Implementation_7_TInterface_Dependant<IInterface_1>
        {
            public Implementation_7_UsingOptionalDependency([Options(Optional = true)] IInterface_1 dep) : base(dep)
            {
            }
        }

        [Test]
        public void Injector_Get_ShouldNotThrowIfAMissingLazyDependencyIsOptional([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_7<Lazy<IInterface_1>>, Implementation_7_UsingOptionalLazyDependency>(Lifetime.Transient), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                IInterface_7<Lazy<IInterface_1>> svc = null;

                Assert.DoesNotThrow(() => svc = injector.Get<IInterface_7<Lazy<IInterface_1>>>());
                Assert.That(svc, Is.Not.Null);
                Assert.That(svc.Dependency, Is.Not.Null);
                Assert.That(svc.Dependency.Value, Is.Null);
            }
        }

        [Test]
        public void Injector_Get_ShouldResolveOptionalLazyDependencies([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                    .Service<IInterface_7<Lazy<IInterface_1>>, Implementation_7_UsingOptionalLazyDependency>(Lifetime.Transient),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

            using (IInjector injector = Root.CreateScope())
            {
                Assert.That(injector.Get<IInterface_7<Lazy<IInterface_1>>>().Dependency.Value, Is.Not.Null);
            }
        }

        private sealed class Implementation_7_UsingOptionalLazyDependency : Implementation_7_TInterface_Dependant<Lazy<IInterface_1>>
        {
            public Implementation_7_UsingOptionalLazyDependency([Options(Optional = true)] Lazy<IInterface_1> dep) : base(dep)
            {
            }
        }

        [Test]
        public void Injector_Get_ShouldNotSpecializeIfTheClosedPairOfTheOpenGenericServiceWasRegistered([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Transient)
                    .Service(typeof(IInterface_3<>), typeof(NotUsedImplementation<>), Lifetime.Transient)
                    .Service<IInterface_3<int>, Implementation_3_IInterface_1_Dependant<int>>(Lifetime.Transient),
                new ScopeOptions { ServiceResolutionMode = resolutionMode }
            );

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

        [Test, Ignore("TBD whether this feature is required or not")]
        public void Injector_Get_ShouldThrowIfItIsCalledInsideDispose()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IMyService, MyServiceUsingItsOnwerInjectorOnDisposal>(Lifetime.Scoped));

            IInjector injector = Root.CreateScope();
            injector.Get<IMyService>();
            Assert.Throws<InvalidOperationException>(injector.Dispose, Resources.INJECTOR_IS_BEING_DISPOSED);
        }

        [Test]
        public void Injector_Get_ShouldCaptureTransientDisposables([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            var mockDisposable = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockDisposable.Setup(d => d.Dispose());

            Root = ScopeFactory.Create(svcs => svcs.Factory<IInterface_1_Disaposable>(factoryExpr: i => mockDisposable.Object, Lifetime.Transient), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                injector.Get<IInterface_1_Disaposable>();
                injector.Get<IInterface_1_Disaposable>();

                IReadOnlyCollection<object> capturedDisposables = injector.Get<IReadOnlyCollection<object>>("captured_disposables");
                Assert.That(capturedDisposables.Count, Is.EqualTo(2));
            }

            mockDisposable.Verify(d => d.Dispose(), Times.Exactly(2));
        }

        [Test]
        public void Injector_Get_ShouldCaptureScopedDisposables([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            var mockDisposable = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockDisposable.Setup(d => d.Dispose());

            Root = ScopeFactory.Create(svcs => svcs.Factory<IInterface_1_Disaposable>(factoryExpr: i => mockDisposable.Object, Lifetime.Scoped), new ScopeOptions { ServiceResolutionMode = resolutionMode });

            using (IInjector injector = Root.CreateScope())
            {
                injector.Get<IInterface_1_Disaposable>();
                injector.Get<IInterface_1_Disaposable>();

                IReadOnlyCollection<object> capturedDisposables = injector.Get<IReadOnlyCollection<object>>("captured_disposables");
                Assert.That(capturedDisposables.Count, Is.EqualTo(1));
            }

            mockDisposable.Verify(d => d.Dispose(), Times.Once);
        }

        [Test]
        public void Injector_Get_ShouldCaptureSingletonDisposablesInTheRoot([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            var mockDisposable = new Mock<IInterface_1_Disaposable>(MockBehavior.Strict);
            mockDisposable.Setup(d => d.Dispose());

            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs.Factory<IInterface_1_Disaposable>(factoryExpr: i => mockDisposable.Object, Lifetime.Singleton), new ScopeOptions { ServiceResolutionMode = resolutionMode }))
            {
                using (IInjector injector = root.CreateScope())
                {
                    injector.Get<IInterface_1_Disaposable>();
                    injector.Get<IInterface_1_Disaposable>();
                }

                using (IInjector injector = root.CreateScope())
                {
                    injector.Get<IInterface_1_Disaposable>();
                    injector.Get<IInterface_1_Disaposable>();
                }

                IReadOnlyCollection<object> capturedDisposables = ((IInjector) root).Get<IReadOnlyCollection<object>>("captured_disposables");
                Assert.That(capturedDisposables.Count, Is.EqualTo(1));
            }

            mockDisposable.Verify(d => d.Dispose(), Times.Once);
        }

        [Test]
        public void Injector_Get_ShouldUpdateTheEntryState([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode)
        {
            AbstractServiceEntry entry = null;

            using (IScopeFactory root = ScopeFactory.Create(svcs => { svcs.Service<IInterface_1, Implementation_1>(lifetime); entry = svcs.Last(); }, ScopeOptions.Default with { ServiceResolutionMode = resolutionMode }))
            {
                Assert.That(entry.State.HasFlag(ServiceEntryStates.Validated), Is.EqualTo(resolutionMode is ServiceResolutionMode.AOT));
                Assert.False(entry.State.HasFlag(ServiceEntryStates.Instantiated));

                using (IInjector injector = root.CreateScope())
                {
                    injector.Get<IInterface_1>();
                }

                Assert.That(entry.State.HasFlag(ServiceEntryStates.Validated));
                Assert.That(entry.State.HasFlag(ServiceEntryStates.Instantiated));
            }
        }

        [Test]
        public void Injector_Get_ShouldUseShortcutForInstantiation([Values(true, false)]bool disposable, [Values(ServiceResolutionMode.AOT, ServiceResolutionMode.JIT)]ServiceResolutionMode resolutionMode)
        {
            IServiceCollection coll = new ServiceCollection().Service
            (
                typeof(IInterface_1),
                disposable
                    ? typeof(Implementation_1_No_Dep_Disposable)
                    : typeof(Implementation_1_No_Dep),
                Lifetime.Transient
            );

            Mock<Injector> mockInjector = new
            (
                coll,
                ScopeOptions.Default with { ServiceResolutionMode = resolutionMode },
                null
            );
            mockInjector
                .Setup(i => i.CreateInstance(coll.Find<IInterface_1>()))
                .CallBase();
            mockInjector
                .Setup(i => i.Get(typeof(IInterface_1), null))
                .CallBase();

            Assert.AreNotSame(mockInjector.Object.Get<IInterface_1>(), mockInjector.Object.Get<IInterface_1>());
            mockInjector.Verify(i => i.CreateInstance(coll.Find<IInterface_1>()), Times.Exactly(disposable ? 2 : 1));
        }
    }
}
