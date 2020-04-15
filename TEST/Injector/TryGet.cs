/********************************************************************************
* TryGet.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    public partial class InjectorTestsBase<TContainer>
    {
        public interface INonExisting { }

        public static IEnumerable<List<(Type Interface, Type Implementation)>> BadRegistrations 
        {
            get 
            {
                yield return new List<(Type Interface, Type Implementation)>
                {
                    (typeof(INonExisting), null)
                };

                yield return new List<(Type Interface, Type Implementation)>
                {
                    (typeof(IInterface_7<INonExisting>), typeof(Implementation_7_TInterface_Dependant<INonExisting>))
                };

                yield return new List<(Type Interface, Type Implementation)>
                {
                    (typeof(IInterface_7<INonExisting>), typeof(Implementation_7_TInterface_Dependant<INonExisting>)),
                    (typeof(IInterface_7<IInterface_7<INonExisting>>), typeof(Implementation_7_TInterface_Dependant<IInterface_7<INonExisting>>))
                };
            }
        }

        [TestCaseSource(nameof(BadRegistrations))]
        public void Injector_TryGet_ShouldReturnNullIfTheServiceCouldNotBeResolved(List<(Type Interface, Type Implementation)> registrations)
        {
            foreach ((Type Interface, Type Implementation) reg in registrations)
                if (reg.Implementation != null) Container.Service(reg.Interface, reg.Implementation);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.That(injector.TryGet(registrations.Last().Interface), Is.Null);
            }
        }

        [Test]
        public void Injector_TryGet_ShouldSupportNamedServices([Values(null, "cica")] string name) 
        {
            Container.Service<IInterface_1, Implementation_1>(name);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.That(injector.TryGet<IInterface_1>(name), Is.InstanceOf<Implementation_1>());
            }
        }
    }
}
