/********************************************************************************
* Transaction.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Extensions.Tests
{
    using Aspects;
    using DI.Tests;
    using Interfaces;

    [TestFixture]
    public class TransactionTests: TestBase<ServiceContainer>
    {
        public interface IModule
        {
            [Transactional]
            void DoSomething(object arg);
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

        [Test]
        public void TransactionHandlingTest()
        {
            var mockTransaction = new Mock<IDbTransaction>(MockBehavior.Strict);
            mockTransaction.Setup(tr => tr.Commit());

            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection
                .Setup(conn => conn.BeginTransaction(IsolationLevel.Unspecified))
                .Returns(mockTransaction.Object);
            mockDbConnection
                .Setup(conn => conn.Dispose());

            Container
                .Factory(i => mockDbConnection.Object, Lifetime.Scoped)
                .Service<IModule, MyModuleUsingDbConnection>(Lifetime.Scoped)
                .Proxy<IModule, TransactionManager<IModule>>();

            using (IInjector injector = Container.CreateInjector())
            {
                IModule module = injector.Get<IModule>();
                module.DoSomething(new object());
                module.DoSomethingElse();
            }

            mockDbConnection.Verify(conn => conn.BeginTransaction(IsolationLevel.Unspecified), Times.Once);
            mockTransaction.Verify(tr => tr.Commit(), Times.Once);
        }

        [TransactionalAspect]
        public interface IModuleWithAspect : IModule { }

        public class MyModuleUsingDbConnectionEx : MyModuleUsingDbConnection, IModuleWithAspect 
        {
            public MyModuleUsingDbConnectionEx(Lazy<IDbConnection> dbConn) : base(dbConn) { }
        }

        [Test]
        public void TransactionHandlingAspectTest() 
        {
            var mockTransaction = new Mock<IDbTransaction>(MockBehavior.Strict);
            mockTransaction.Setup(tr => tr.Commit());

            var mockDbConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockDbConnection
                .Setup(conn => conn.BeginTransaction(IsolationLevel.Unspecified))
                .Returns(mockTransaction.Object);
            mockDbConnection
                .Setup(conn => conn.Dispose());

            Container
                .Factory(i => mockDbConnection.Object, Lifetime.Scoped)
                .Service<IModuleWithAspect, MyModuleUsingDbConnectionEx>(Lifetime.Scoped);

            using (IInjector injector = Container.CreateInjector())
            {
                IModuleWithAspect module = injector.Get<IModuleWithAspect>();
                module.DoSomething(new object());
                module.DoSomethingElse();
            }

            mockDbConnection.Verify(conn => conn.BeginTransaction(IsolationLevel.Unspecified), Times.Once);
            mockTransaction.Verify(tr => tr.Commit(), Times.Once);
        }
    }
}
