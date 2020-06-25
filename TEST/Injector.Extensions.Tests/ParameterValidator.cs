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
    using Proxy;

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

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public class MinLengthAttribute : ParameterValidatorAttribute
        {
            public int Value { get; set; }

            public override void Validate(ParameterInfo param, object value)
            {
                if (((string) value ?? "").Length < Value) throw new ArgumentException($"The length of the string should be at least {Value}");
            }
        }

        public interface IModule
        {
            void DoSomething([NotNull, MinLength(Value = 1)] string arg);
            void DoSomethingElse();
        }

        [Test]
        public void ParameterValidationTest()
        {
            Container
                .Factory(i => new Mock<IModule>().Object)
                .Proxy(typeof(IModule), (_, __, instance) => ProxyFactory.Create(typeof(IModule), typeof(ParameterValidator<IModule>), instance));

            using (IInjector injector = Container.CreateInjector())
            {
                IModule module = injector.Get<IModule>();

                Assert.DoesNotThrow(() => module.DoSomething("cica"));
                Assert.Throws<ArgumentNullException>(() => module.DoSomething(null));
            }
        }

        [ParameterValidatorAspect(aggregate: true)]
        public interface IModuleWithAspect : IModule { }

        [Test]
        public void ParameterValidationAspectTest()
        {
            Container.Factory(i => new Mock<IModuleWithAspect>().Object);

            using (IInjector injector = Container.CreateInjector())
            {
                IModule module = injector.Get<IModuleWithAspect>();

                Assert.DoesNotThrow(() => module.DoSomething("cica"));
                var ex = Assert.Throws<AggregateException>(() => module.DoSomething(null));
                Assert.That(ex.InnerExceptions.Count, Is.EqualTo(2));
            }
        }
    }
}
