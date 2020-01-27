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
            var dep = new Implementation_1_No_Dep();

            using (IInjector injector = Container.CreateInjector())
            {
                Implementation_2_IInterface_1_Dependant obj = injector.Instantiate<Implementation_2_IInterface_1_Dependant>(new Dictionary<string, object>
                {
                    {"interface1", dep}
                });

                Assert.That(obj.Interface1, Is.SameAs(dep));
            }
        }

        [Test]
        public void Injector_Instantiate_ShouldResolveDependencies()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>();

            using (IInjector injector = Container.CreateInjector())
            {
                Implementation_2_IInterface_1_Dependant obj = injector.Instantiate<Implementation_2_IInterface_1_Dependant>();
                Assert.That(obj.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Instantiate_ShouldThrowOnOpenGenericType()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<ArgumentException>(() => injector.Instantiate(typeof(Implementation_3_IInterface_1_Dependant<>)), Resources.CANT_INSTANTIATE_GENERICS);
            }           
        }

        [Test]
        public void Injector_Instantiate_ShouldWorkWithClosedGenericTypes()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>();

            using (IInjector injector = Container.CreateInjector())
            {
                Implementation_3_IInterface_1_Dependant<string> obj = injector.Instantiate<Implementation_3_IInterface_1_Dependant<string>>();
                Assert.That(obj.Interface1, Is.InstanceOf<Implementation_1_No_Dep>());
            }
        }

        [Test]
        public void Injector_Instantiate_ShouldTakeTheServiceActivatorAttributeIntoAccount()
        {
            using (IInjector injector = Container.CreateInjector())
            {
                Assert.DoesNotThrow(() => injector.Instantiate<Implementation_8_MultiCtor>());
            }          
        }

        [Test]
        public void Injector_Instantiate_ShouldValidate() 
        {
            Assert.Throws<ArgumentNullException>(() => IInjectorExtensions.Instantiate(null, typeof(object)));

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<ArgumentNullException>(() => injector.Instantiate(null));
                Assert.Throws<ArgumentException>(() => injector.Instantiate(typeof(IInjector)));
            }
        }
    }
}
