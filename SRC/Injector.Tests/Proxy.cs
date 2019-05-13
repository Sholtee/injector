using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    [TestFixture]
    public sealed partial class InjectorTests
    {
        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_Proxy_ShouldOverwriteTheFactoryFunction(Lifetime lifetime)
        {
            int
                callbackCallCount = 0,
                typedCallbackCallCount = 0;

            Injector
                .Service<IInterface_1, Implementation_1>(lifetime)
                .Proxy(typeof(IInterface_1), (injector, t, inst) =>
                {
                    Assert.AreSame(injector, Injector);
                    Assert.That(t, Is.EqualTo(typeof(IInterface_1)));
                    Assert.That(inst, Is.InstanceOf<Implementation_1>());

                    typedCallbackCallCount++;
                    return inst;
                })
                .Proxy<IInterface_1>((injector, inst) =>
                {
                    Assert.AreSame(injector, Injector);
                    Assert.That(inst, Is.TypeOf<Implementation_1>());
                    
                    callbackCallCount++;
                    return new DecoratedImplementation_1();
                });

            var instance = Injector.Get<IInterface_1>();
            
            Assert.That(instance, Is.InstanceOf<DecoratedImplementation_1>());
            Assert.That(typedCallbackCallCount, Is.EqualTo(1));
            Assert.That(callbackCallCount, Is.EqualTo(1));

            (lifetime == Lifetime.Singleton ? Assert.AreSame : ((Action<object, object>) Assert.AreNotSame))(instance, Injector.Get<IInterface_1>());
        }

        [Test]
        public void Injector_Proxy_ShouldWorkWithGenericTypes()
        {
            int callbackCallCount = 0;

            Injector
                .Service(typeof(IInterface_3<>), typeof(Implementation_3<>))
                .Proxy(typeof(IInterface_3<>), (injector, type, inst) =>
                {
                    Assert.AreSame(injector, Injector);
                    Assert.AreSame(type, typeof(IInterface_3<int>));
                    Assert.That(inst, Is.InstanceOf<Implementation_3<int>>());

                    callbackCallCount++;
                    return new DecoratedImplementation_3<int>();
                });

            var instance = Injector.Get<IInterface_3<int>>();
            
            Assert.That(instance, Is.InstanceOf<DecoratedImplementation_3<int>>());
            Assert.That(callbackCallCount, Is.EqualTo(1));
        }
    }
}
