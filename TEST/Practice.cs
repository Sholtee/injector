/********************************************************************************
* Practice.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;
using System.Linq;
using System.Reflection;

using NUnit.Framework;
using Moq;

namespace Solti.Utils.DI.Practice
{
    using Annotations;
    using Internals;
    using DI;
    using Proxy;
    using Tests;

    [TestFixture]
    public class Practice: TestBase<ServiceContainer>
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
                module.DoSomething();
            }

            mockDbConnection.Verify(conn => conn.BeginTransaction(), Times.Once);
            mockTransaction.Verify(tr => tr.Commit(), Times.Once);
        }

        public interface IModule
        {
            void DoSomething();
        }

        public class MyModuleUsingDbConnection : IModule
        {
            public IDbConnection Connection { get; }
            public MyModuleUsingDbConnection(IDbConnection dbConn)
            {
                Assert.NotNull(dbConn);
                Connection = dbConn;
            }
            public void DoSomething() { }
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
                .Setup(typeof(Practice).Assembly())
                //
                // Ne Transient legyen mert ott a szerviz proxy lesz ha az implementacio
                // IDisposable leszarmazott.
                //

                .Service<IDisposable, Disposable>(Lifetime.Scoped);

            foreach (AbstractServiceEntry entry in Container.Where(e => typeof(IModule).IsAssignableFrom(e.Interface)))
                Container.Proxy(entry.Interface, typeof(InterfaceInterceptor<>).MakeGenericType(entry.Interface));

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            Assert.That(Container.Get<IDisposable>().GetService(() => mockInjector.Object) is Disposable);
            Assert.That(Container.Get<IMyModule1>().GetService(() => mockInjector.Object) is InterfaceInterceptor<IMyModule1>);
            Assert.That(Container.Get<IMyModule2>().GetService(() => mockInjector.Object) is InterfaceInterceptor<IMyModule2>);
        }

        public interface IMyModule1 : IModule { }
        public interface IMyModule2 : IModule { }
        [Service(typeof(IMyModule1))]
        public class Module1 : IMyModule1 
        {
            public void DoSomething() { }
        }
        [Service(typeof(IMyModule2))]
        public class Module2 : IMyModule2 
        {
            public void DoSomething() { }
        }
    }
}
