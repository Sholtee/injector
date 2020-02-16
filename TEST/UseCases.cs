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
    using Annotations;

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
            }

            mockDbConnection.Verify(conn => conn.BeginTransaction(), Times.Once);
            mockTransaction.Verify(tr => tr.Commit(), Times.Once);
        }

        public interface IModule
        {
            void DoSomething([NotNull] object arg);
        }

        public class MyModuleUsingDbConnection : IModule
        {
            public IDbConnection Connection { get; }
            public MyModuleUsingDbConnection(IDbConnection dbConn)
            {
                Assert.NotNull(dbConn);
                Connection = dbConn;
            }
            public void DoSomething(object arg) { }
        }

        public class TransactionManager<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
        {
            public IDbConnection Connection { get; }

            public TransactionManager(TInterface target, IDbConnection dbConn) : base(target)
            {
                Assert.NotNull(dbConn);
                Connection = dbConn;
            }

            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
            {
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
                .Setup(typeof(Tests).Assembly())
                //
                // Ne Transient legyen mert ott a szerviz proxy lesz ha az implementacio
                // IDisposable leszarmazott.
                //

                .Service<IDisposable, Disposable>(Lifetime.Scoped);

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
                ServiceReference svc = new ServiceReference(new AbstractServiceEntry(typeof(TInterface), null), mockInjector.Object);
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
        }
        [Service(typeof(IMyModule2))]
        public class Module2 : IMyModule2 
        {
            public void DoSomething(object arg) { }
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
    }
}
