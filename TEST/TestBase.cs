﻿/********************************************************************************
* TestBase.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Annotations;

    public class TestBase
    {
        protected IServiceContainer Container;

        [SetUp]
        public void SetupTest() => Container = ServiceContainer.Create();

        [TearDown]
        public void TearDown()
        {
            Container.Dispose();
            Container = null;
        }

        public interface IInterface_1
        {
        }

        public class Implementation_1 : IInterface_1 // nincs konstruktor definialva
        {
        }

        public class Implementation_1_Invalid : IInterface_1
        {
            public Implementation_1_Invalid(int invalidArg)
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

        public class Implementation_2 : IInterface_2
        {
            public Implementation_2(IInterface_1 interface1)
            {
                Interface1 = interface1;
            }

            public IInterface_1 Interface1 { get; }
        }

        public class Implementation_2_LazyDep : IInterface_2_LazyDep
        {
            public Implementation_2_LazyDep(Lazy<IInterface_1> interface1)
            {
                Interface1 = interface1;
            }

            public Lazy<IInterface_1> Interface1 { get; }
        }

        public interface IInterface_3<T>
        {
            IInterface_1 Interface1 { get; }
        }

        public class Implementation_3<T> : IInterface_3<T>
        {
            public Implementation_3(IInterface_1 interface1)
            {
                Interface1 = interface1;
            }

            public IInterface_1 Interface1 { get; }
        }

        public class DecoratedImplementation_3<T> : Implementation_3<T>
        {
            public DecoratedImplementation_3() : base(null)
            {
            }
        }

        public interface IInterface_4
        {
        }

        public class Implementation_4_cdep : IInterface_4
        {
            public Implementation_4_cdep(IInterface_5 dep)
            {
            }
        }

        public interface IInterface_5
        {
        }

        public class Implementation_5_cdep : IInterface_5
        {
            public Implementation_5_cdep(IInterface_4 dep)
            {     
            }
        }

        public interface IInterface_6<T>
        {
            IInterface_3<T> Interface3 { get; }
        }

        public class Implementation_6<T> : IInterface_6<T>
        {
            public Implementation_6(IInterface_3<T> dep)
            {
                Interface3 = dep;
            }

            public IInterface_3<T> Interface3 { get; }
        }

        public class Implementation_7_cdep : IInterface_1
        {
            public Implementation_7_cdep(IInjector injector)
            {
                injector.Get<IInterface_4>(); // cdep
            }
        }

        public class Implementation_8_multictor : IInterface_1
        {
            [ServiceActivator]
            public Implementation_8_multictor()
            {                
            }

            public Implementation_8_multictor(IInterface_2 useless)
            {                
            }
        }

        public class Implementation_9_multictor<T> : IInterface_3<T>
        {        
            public Implementation_9_multictor(int useless)
            {
            }

            [ServiceActivator]
            public Implementation_9_multictor(IInterface_1 dep)
            {
                Interface1 = dep;
            }

            public IInterface_1 Interface1 { get; }
        }

        public interface IInterface_1_Disaposable : IInterface_1, IDisposable
        {
        }

        public interface IInterface_2_Disaposable : IInterface_2, IDisposable
        {
        }
    }
}
