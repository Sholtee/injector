/********************************************************************************
* InjectorTests.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;
    using Properties;

    [TestFixture]
    public partial class InjectorTests
    {
        #region Lifetimes
        public static IEnumerable<LifetimeBase> Lifetimes
        {
            get
            {
                yield return Lifetime.Transient;
                yield return Lifetime.Scoped;
                yield return Lifetime.Singleton;
                yield return Lifetime.Pooled.Using(PoolConfig.Default with { Capacity = 4 });
            }
        }

        public static IEnumerable<Lifetime> ScopeControlledLifetimes
        {
            get
            {
                yield return Lifetime.Transient;
                yield return Lifetime.Scoped;
            }
        }

        public static IEnumerable<Lifetime> RootControlledLifetimes
        {
            get
            {
                yield return Lifetime.Pooled;
                yield return Lifetime.Singleton;
            }
        }

        public static IEnumerable<ServiceResolutionMode> ResolutionModes
        {
            get
            {
                yield return ServiceResolutionMode.JIT;
                yield return ServiceResolutionMode.AOT;
            }
        }
        #endregion

        #region Services
        public interface IInterface_1
        {
        }

        public class Implementation_1_No_Dep : IInterface_1 // nincs konstruktor definialva
        {
        }

        public class Implementation_1_Non_Interface_Dep : IInterface_1
        {
            public Implementation_1_Non_Interface_Dep(int invalidArg)
            {
            }
        }

        public class DecoratedImplementation_1 : IInterface_1
        {
        }

        public interface IInterface_2
        {
            IInterface_1 Interface1 { get; }
        }

        public interface IInterface_2_LazyDep
        {
            Lazy<IInterface_1> Interface1 { get; }
        }

        public class Implementation_2_IInterface_1_Dependant : IInterface_2
        {
            public Implementation_2_IInterface_1_Dependant(IInterface_1 interface1)
            {
                Interface1 = interface1;
            }

            public IInterface_1 Interface1 { get; }
        }

        public class Implementation_2_Lazy__IInterface_1_Dependant : IInterface_2_LazyDep
        {
            public Implementation_2_Lazy__IInterface_1_Dependant(Lazy<IInterface_1> interface1)
            {
                Interface1 = interface1;
            }

            public Lazy<IInterface_1> Interface1 { get; }
        }

        public interface IInterface_3<T>
        {
            IInterface_1 Interface1 { get; }
        }

        public class Implementation_3_IInterface_1_Dependant<T> : IInterface_3<T>
        {
            public Implementation_3_IInterface_1_Dependant(IInterface_1 interface1)
            {
                Interface1 = interface1;
            }

            public IInterface_1 Interface1 { get; }
        }

        public class DecoratedImplementation_3<T> : Implementation_3_IInterface_1_Dependant<T>
        {
            public DecoratedImplementation_3() : base(null)
            {
            }
        }

        public interface IInterface_4
        {
        }

        public class Implementation_4_CDep : IInterface_4
        {
            public Implementation_4_CDep(IInterface_5 dep)
            {
            }
        }

        public interface IInterface_5
        {
        }

        public class Implementation_5_CDep : IInterface_5
        {
            public Implementation_5_CDep(IInterface_4 dep)
            {
            }
        }

        public interface IInterface_6<T>
        {
            IInterface_3<T> Interface3 { get; }
        }

        public interface IInterface_7<TInterface> where TInterface : class
        {
            TInterface Interface { get; }
        }

        public class Implementation_7<TInterface>: IInterface_7<TInterface> where TInterface : class
        {
            public TInterface Interface { get; }

            public Implementation_7(TInterface iface) => Interface = iface;
        }

        public interface IInterface_7_Disposable<TInterface> : IDisposableEx where TInterface : class
        {
            TInterface Interface { get; }
        }

        public class Implementation_6_IInterface_3_Dependant<T> : IInterface_6<T>
        {
            public Implementation_6_IInterface_3_Dependant(IInterface_3<T> dep)
            {
                Interface3 = dep;
            }

            public IInterface_3<T> Interface3 { get; }
        }

        public class Implementation_7_CDep : IInterface_1
        {
            public Implementation_7_CDep(IInjector injector)
            {
                injector.Get<IInterface_4>(); // cdep
            }
        }

        public class Implementation_8_MultiCtor : IInterface_1
        {
            [ServiceActivator]
            public Implementation_8_MultiCtor()
            {
            }

            public Implementation_8_MultiCtor(IInterface_2 useless)
            {
            }
        }

        public class Implementation_9_MultiCtor<T> : IInterface_3<T>
        {
            public Implementation_9_MultiCtor(int useless)
            {
            }

            [ServiceActivator]
            public Implementation_9_MultiCtor(IInterface_1 dep)
            {
                Interface1 = dep;
            }

            public IInterface_1 Interface1 { get; }
        }

        public class Implementation_7_TInterface_Dependant<TInterface> : IInterface_7<TInterface> where TInterface : class
        {
            public Implementation_7_TInterface_Dependant(TInterface dep)
            {
                Interface = dep;
            }

            public TInterface Interface { get; }
        }

        public class Implementation_10_RecursiveCDep : IInterface_1
        {
            public Implementation_10_RecursiveCDep(IInterface_1 dep) { }
        }

        public interface IInterface_1_Disaposable : IInterface_1, IDisposable
        {
        }

        public interface IInterface_2_Disaposable : IInterface_2, IDisposable
        {
        }
        #endregion

        public IScopeFactory Root { get; set; }

        [TearDown]
        public void TearDwon()
        {
            Root?.Dispose();
            Root = null;
        }

        [Test]
        public void Ctor_ShouldThrowOnOverriddenService([ValueSource(nameof(ResolutionModes))] ServiceResolutionMode resolutionMode) =>
            Assert.Throws<ServiceAlreadyRegisteredException>(() => ScopeFactory.Create(svcs => svcs.Add(new InstanceServiceEntry(typeof(IInjector), null, new Mock<IInjector>().Object, ServiceOptions.Default)), new ScopeOptions { ServiceResolutionMode = resolutionMode }), Resources.SERVICE_ALREADY_REGISTERED);
    }
}
