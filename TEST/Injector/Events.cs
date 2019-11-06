/********************************************************************************
* Events.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void Injector_OnServiceRequest_ShouldBeFiredBeforeTheServiceRequest()
        {
            bool serviceCreated = false;

            Container.Factory<IInterface_1>(injector =>
            {
                serviceCreated = true;
                return new Implementation_1_No_Dep();
            });

            using (IInjector injector = Container.CreateInjector())
            {
                bool eventFired = true;

                injector.OnServiceRequest += (sender, arg) =>
                {
                    eventFired = true;

                    Assert.That(serviceCreated, Is.False);
                    Assert.That(arg.Interface, Is.SameAs(typeof(IInterface_1)));
                    Assert.That(arg.Service, Is.Null);
                };

                injector.Get<IInterface_1>();
                Assert.That(eventFired);
            }
        }

        [Test]
        public void Injector_OnServiceRequest_ShouldBeFiredAfterTheServiceRequest()
        {
            bool serviceCreated = false;

            Container.Factory<IInterface_1>(injector =>
            {
                serviceCreated = true;
                return new Implementation_1_No_Dep();
            });

            using (IInjector injector = Container.CreateInjector())
            {
                bool eventFired = true;

                injector.OnServiceRequested += (sender, arg) =>
                {
                    eventFired = true;

                    Assert.That(serviceCreated, Is.True);
                    Assert.That(arg.Interface, Is.SameAs(typeof(IInterface_1)));
                    Assert.That(arg.Service, Is.InstanceOf<Implementation_1_No_Dep>());
                };

                injector.Get<IInterface_1>();
                Assert.That(eventFired);
            }
        }

        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_OnServiceRequest_CanChangeTheReturnedService(Lifetime lifetime)
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                var myImplementation = new Implementation_1_No_Dep();

                bool eventFired = false;

                injector.OnServiceRequest += (sender, arg) =>
                {
                    eventFired = true;

                    Assert.AreSame(injector, sender);
                    Assert.That(arg.Service, Is.Null);
                    Assert.That(arg.Interface, Is.SameAs(typeof(IInterface_1)));

                    arg.Service = myImplementation;
                };
            
                Assert.That(injector.Get<IInterface_1>(), Is.SameAs(myImplementation));
                Assert.That(eventFired);
                Assert.That(injector.Get<IInterface_1>(), Is.SameAs(myImplementation));
            }
        }

        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_OnServiceRequested_CanChangeTheReturnedService(Lifetime lifetime)
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime);

            using (IInjector injector = Container.CreateInjector())
            {
                var myImplementation = new Implementation_1_No_Dep();

                bool eventFired = false;

                injector.OnServiceRequested += (sender, arg) =>
                {
                    eventFired = true;

                    Assert.AreSame(injector, sender);
                    Assert.That(arg.Service, Is.InstanceOf<Implementation_1_No_Dep>());
                    Assert.That(arg.Interface, Is.SameAs(typeof(IInterface_1)));

                    arg.Service = myImplementation;
                };

                Assert.That(injector.Get<IInterface_1>(), Is.SameAs(myImplementation));
                Assert.That(eventFired);
                Assert.That(injector.Get<IInterface_1>(), Is.SameAs(myImplementation));
            }
        }
    }
}
