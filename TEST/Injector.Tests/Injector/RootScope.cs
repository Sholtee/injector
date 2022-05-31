/********************************************************************************
* RootScope.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Internals;

    public partial class InjectorTests
    {
        [Test]
        public void Injector_Create_ShouldThrowOnNonRegisteredDependency([ValueSource(nameof(Lifetimes))] Lifetime lifetime, [ValueSource(nameof(Engines))] string engine)
        {
            var ex = Assert.Throws<ServiceNotFoundException>(() => ScopeFactory.Create(svcs => svcs.Service<IInterface_7<IInterface_1>, Implementation_7_TInterface_Dependant<IInterface_1>>(lifetime), new ScopeOptions { Engine = engine, ServiceResolutionMode = ServiceResolutionMode.AOT }));

            Assert.That(ex.Data["requested"], Is.InstanceOf<MissingServiceEntry>().And.EqualTo(new MissingServiceEntry(typeof(IInterface_1), null)).Using(ServiceIdComparer.Instance));
            
            //
            // FIXME:
            //

            //Assert.That(ex.Data["requestor"], Is.EqualTo(new DummyServiceEntry(typeof(IInterface_7<IInterface_1>), null)).Using(ServiceIdComparer.Instance));
        }
    }
}
