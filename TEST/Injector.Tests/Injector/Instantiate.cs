﻿/********************************************************************************
* Instantiate.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Interfaces;
    using Properties;
    
    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void Injector_Instantiate_ShouldUseTheCurrentInjector()
        {
            new List<IServiceContainer> {Container, Container.CreateChild()}.ForEach(container =>
            {
                using (IInjector injector = container.CreateInjector())
                {
                    Implementation_7_TInterface_Dependant<IInjector> obj = injector.Instantiate<Implementation_7_TInterface_Dependant<IInjector>>();
                    Assert.That(obj.Interface, Is.SameAs(injector));
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

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Instantiate_ShouldResolveDependencies(Lifetime lifetime)
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime);

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
                Assert.Throws<ArgumentException>(() => injector.Instantiate(typeof(Implementation_3_IInterface_1_Dependant<>)), Resources.PARAMETER_IS_GENERIC);
            }           
        }

        [TestCaseSource(nameof(Lifetimes))]
        public void Injector_Instantiate_ShouldWorkWithClosedGenericTypes(Lifetime lifetime)
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>(lifetime);

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
            Assert.Throws<ArgumentNullException>(() => IInjectorAdvancedExtensions.Instantiate(null, typeof(object)));

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<ArgumentNullException>(() => injector.Instantiate(null));
                Assert.Throws<ArgumentException>(() => injector.Instantiate(typeof(IInjector)));
            }
        }
    }
}
