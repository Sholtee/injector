/********************************************************************************
* TestBase.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    [TestFixture]
    public sealed partial class InjectorTests
    {
        private IInjector Injector;

        [SetUp]
        public void SetupTest()
        {
            Injector = DI.Injector.Create();
        }

        [TearDown]
        public void TearDown()
        {
            Injector.Dispose();
            Injector = null;
        }

        public interface IInterface_1
        {
        }

        public class Implementation_1 : IInterface_1
        {
        }

        public class DecoratedImplementation_1 : IInterface_1
        {
        }

        public interface IInterface_2
        {
            IInterface_1 Interface1 { get; }
        }

        public class Implementation_2 : IInterface_2
        {
            public Implementation_2(IInterface_1 interface1)
            {
                Interface1 = interface1;
            }

            public IInterface_1 Interface1 { get; }
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
    }
}
