/********************************************************************************
* ParameterValidator.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Extensions.Tests
{
    using Aspects;
    using DI.Tests;
    using Interfaces;

    [TestFixture]
    public class ParameterValidatorTests: TestBase<ServiceContainer>
    {
        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public class NotNullAttribute : ParameterValidatorAttribute
        {
            public override void Validate(ParameterInfo param, object value)
            {
                if (value == null) throw new ArgumentNullException(param.Name);
            }
        }
    
        public interface IModule
        {
            void DoSomething([NotNull] object arg);
            void DoSomethingElse();
        }

        [Test]
        public void ParameterValidationTest()
        {
            Container
                .Factory(i => new Mock<IModule>().Object)
                .Proxy<IModule, ParameterValidator<IModule>>();

            using (IInjector injector = Container.CreateInjector())
            {
                IModule module = injector.Get<IModule>();

                Assert.DoesNotThrow(() => module.DoSomething(new object()));
                Assert.Throws<ArgumentNullException>(() => module.DoSomething(null));
            }
        }

        [ParameterValidatorAspect]
        public interface IModuleWithAspect : IModule { }

        [Test]
        public void ParameterValidationAspectTest()
        {
            Container.Factory(i => new Mock<IModuleWithAspect>().Object);

            using (IInjector injector = Container.CreateInjector())
            {
                IModule module = injector.Get<IModuleWithAspect>();

                Assert.DoesNotThrow(() => module.DoSomething(new object()));
                Assert.Throws<ArgumentNullException>(() => module.DoSomething(null));
            }
        }
    }
}
