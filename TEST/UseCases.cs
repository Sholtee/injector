/********************************************************************************
* UseCases.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;
using System.Linq;
using System.Reflection;

using NUnit.Framework;
using Moq;

namespace Solti.Utils.DI.UseCases
{  
    using Internals;

    using DI.Tests;

    using Utils.Proxy;

    [TestFixture]
    public class Tests: TestBase<ServiceContainer>
    {
        [Test]
        public void TransactionHandlingTest()
        {
            var mockTransaction = new Mock<IDbTransaction>(MockBehavior.Strict);
            mockTransaction.Setup(tr => tr.Commit());

            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection
                .Setup(conn => conn.BeginTransaction())
                .Returns(mockTransaction.Object);
            mockDbConnection
                .Setup(conn => conn.Dispose());

            Container
                .Factory<IDbConnection>(i => mockDbConnection.Object, Lifetime.Scoped)
                .Service<IModule, MyModuleUsingDbConnection>(Lifetime.Scoped)
                .Proxy<IModule, TransactionManager<IModule>>();

            using (IInjector injector = Container.CreateInjector())
            {
                IModule module = injector.Get<IModule>();
                module.DoSomething(new object());
                module.DoSomethingElse();
            }

            mockDbConnection.Verify(conn => conn.BeginTransaction(), Times.Once);
            mockTransaction.Verify(tr => tr.Commit(), Times.Once);
        }

        public interface IModule
        {
            [Transactional]
            void DoSomething([NotNull] object arg);
            void DoSomethingElse();
        }

        public class MyModuleUsingDbConnection : IModule
        {
            private readonly Lazy<IDbConnection> FConnection;
            public IDbConnection Connection => FConnection.Value;

            public MyModuleUsingDbConnection(Lazy<IDbConnection> dbConn)
            {
                Assert.NotNull(dbConn);
                FConnection = dbConn;
            }
            public void DoSomething(object arg) { }

            public void DoSomethingElse() { }
        }

        [AttributeUsage(AttributeTargets.Method)]
        public class TransactionalAttribute : Attribute { }

        public class TransactionManager<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
        {
            private readonly Lazy<IDbConnection> FConnection;

            public IDbConnection Connection => FConnection.Value;

            public TransactionManager(TInterface target, Lazy<IDbConnection> dbConn) : base(target)
            {
                Assert.NotNull(dbConn);
                FConnection = dbConn;
            }

            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
            {
                if (method.GetCustomAttribute<TransactionalAttribute>() == null) 
                    return base.Invoke(method, args, extra);

                IDbTransaction transaction = Connection.BeginTransaction();
                try
                {
                    object result = base.Invoke(method, args, extra);
                    transaction.Commit();
                    return result;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        [Test]
        public void BulkedProxyingTest()
        {
            Container
                .Setup(typeof(Tests).Assembly)
                .Service<IDisposable, Disposable>();

            foreach (AbstractServiceEntry entry in Container.Where(e => typeof(IModule).IsAssignableFrom(e.Interface)))
                Container.Proxy(entry.Interface, typeof(InterfaceInterceptor<>).MakeGenericType(entry.Interface));

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .SetupGet(i => i.UnderlyingContainer)
                .Returns(Container);

            Assert.That(GetService<IDisposable>() is Disposable);
            Assert.That(GetService<IMyModule1>() is InterfaceInterceptor<IMyModule1>);
            Assert.That(GetService<IMyModule2>() is InterfaceInterceptor<IMyModule2>);

            object GetService<TInterface>() 
            {
                ServiceReference svc = new ServiceReference(Container.Get<TInterface>(), mockInjector.Object);
                Container.Get<TInterface>().SetInstance(svc, FactoryOptions);

                return svc.Value;
            }
        }

        public interface IMyModule1 : IModule { }
        public interface IMyModule2 : IModule { }
        [Service(typeof(IMyModule1))]
        public class Module1 : IMyModule1 
        {
            public void DoSomething(object arg) { }
            public void DoSomethingElse() { }
        }
        [Service(typeof(IMyModule2))]
        public class Module2 : IMyModule2 
        {
            public void DoSomething(object arg) { }
            public void DoSomethingElse() { }
        }

        [Test]
        public void ParameterValidationTest()
        {
            Container
                .Factory<IModule>(i => new Mock<IModule>().Object)
                .Proxy<IModule, ParameterValidator<IModule>>();

            using (IInjector injector = Container.CreateInjector()) 
            {
                IModule module = injector.Get<IModule>();

                Assert.DoesNotThrow(() => module.DoSomething(new object()));
                Assert.Throws<ArgumentNullException>(() => module.DoSomething(null));
            }
        }

        [Test]
        public void ParameterValidationAspectTest() 
        {
            Container.Factory<IModuleWithAspects>(i => new Mock<IModuleWithAspects>().Object);

            using (IInjector injector = Container.CreateInjector())
            {
                IModule module = injector.Get<IModuleWithAspects>();

                Assert.DoesNotThrow(() => module.DoSomething(new object()));
                Assert.Throws<ArgumentNullException>(() => module.DoSomething(null));
            }
        }

        [ParameterValidatorAspect]
        public interface IModuleWithAspects : IModule 
        { 
        }

        public abstract class ParameterValidatorAttribute: Attribute
        {
            public abstract void Validate(ParameterInfo param, object value);
        }

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public class NotNullAttribute : ParameterValidatorAttribute
        {
            public override void Validate(ParameterInfo param, object value)
            {
                if (value == null) throw new ArgumentNullException(param.Name);
            }
        }

        public class ParameterValidator<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
        {
            public ParameterValidator(TInterface target) : base(target)
            { 
            }

            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
            {
                foreach(var ctx in method.GetParameters().Select(
                    (p, i) => new 
                    { 
                        Parameter = p, 
                        Value = args[i], 
                        Validators = p.GetCustomAttributes<ParameterValidatorAttribute>() 
                    }))
                {
                    foreach (var validator in ctx.Validators) 
                    {
                        validator.Validate(ctx.Parameter, ctx.Value);
                    }
                }

                return base.Invoke(method, args, extra);
            }
        }

        [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
        public sealed class ParameterValidatorAspect : AspectAttribute
        {
            public override Type GetInterceptor(Type iface) => typeof(ParameterValidator<>).MakeGenericType(iface);
        }

        [Test]
        public void ContextualDependencyTest() 
        {
            Container.Service<IModule, ModuleHavingContextualDep>(Lifetime.Scoped);

            using (IInjector injector = Container.CreateInjector()) 
            {
                injector
                    .UnderlyingContainer
                    .Instance(new FakeHttpRequest().Act().Like<IHttpRequest>());

                Assert.DoesNotThrow(() => injector.Get<IModule>());
            }
        }

        public interface IHttpRequest  // publikusnak kell lennie
        {
            string this[string key] { get; }
        }

        public sealed class FakeHttpRequest // publikusnak kell lennie
        {
            public string this[string key] { get => key; }
        }

        private sealed class ModuleHavingContextualDep : IModule
        {
            public ModuleHavingContextualDep(IHttpRequest httpRequest) 
            {
                Assert.That(httpRequest, Is.Not.Null);
                Assert.That(httpRequest["cica"], Is.EqualTo("cica"));
            }

            public void DoSomething(object arg) {}

            public void DoSomethingElse() {}
        }
    }
}
