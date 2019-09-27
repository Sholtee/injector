/********************************************************************************
* Instantiate.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Properties;
    
    public partial class InjectorTestsBase<TContainer>
    {
        private sealed class MyClass
        {
            public IInjector Injector { get; }
            public MyClass(IInjector injector) => Injector = injector;
        }

        [Test]
        public void Injector_Instantiate_ShouldUseTheCurrentInjector()
        {
            new List<IServiceContainer> {Container, Container.CreateChild()}.ForEach(container =>
            {
                using (IInjector injector = container.CreateInjector())
                {
                    MyClass obj = injector.Instantiate<MyClass>();
                    Assert.That(obj.Injector, Is.SameAs(injector));
                }
            });
        }

        [Test]
        public void Injector_Instantiate_ShouldAcceptExplicitArguments()
        {
            var dep = new Implementation_1();

            using (IInjector injector = Container.CreateInjector())
            {
                Implementation_2 obj = injector.Instantiate<Implementation_2>(new Dictionary<string, object>
                {
                    {"interface1", dep}
                });

                Assert.That(obj.Interface1, Is.SameAs(dep));
            }
        }

        [Test]
        public void Injector_Instantiate_ShouldResolveDependencies()
        {
            Container.Service<IInterface_1, Implementation_1>();

            using (IInjector injector = Container.CreateInjector())
            {
                Implementation_2 obj = injector.Instantiate<Implementation_2>();
                Assert.That(obj.Interface1, Is.InstanceOf<Implementation_1>());
            }
        }

        [Test]
        public void Injector_Instantiate_ShouldThrowOnOpenGenericType()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<ArgumentException>(() => injector.Instantiate(typeof(Implementation_3<>)), Resources.CANT_INSTANTIATE_GENERICS);
            }           
        }

        [Test]
        public void Injector_Instantiate_ShouldWorkWithClosedGenericTypes()
        {
            Container.Service<IInterface_1, Implementation_1>();

            using (IInjector injector = Container.CreateInjector())
            {
                Implementation_3<string> obj = injector.Instantiate<Implementation_3<string>>();
                Assert.That(obj.Interface1, Is.InstanceOf<Implementation_1>());
            }
        }

        [Test]
        public void Injector_Instantiate_ShouldTakeIntoAccountTheServiceActivatorAttribute()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                Assert.DoesNotThrow(() => injector.Instantiate<Implementation_8_multictor>());
            }          
        }
    }
}
