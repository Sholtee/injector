/********************************************************************************
* UseCases.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.Extensions.Configuration;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.UseCases
{
    using Interfaces;
    using Internals;
    using Extensions.Aspects;
    using Primitives.Patterns;
    using Proxy;
    using DI.Tests;

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

        [Test]
        public void BulkedProxyingTest()
        {
            Container
                .Setup(typeof(Tests).Assembly, "Solti.Utils.DI.UseCases")
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
                .Factory(i => new Mock<IModule>().Object)
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
            Container.Factory(i => new Mock<IModuleWithAspects>().Object);

            using (IInjector injector = Container.CreateInjector())
            {
                IModule module = injector.Get<IModuleWithAspects>();

                Assert.DoesNotThrow(() => module.DoSomething(new object()));
                Assert.Throws<ArgumentNullException>(() => module.DoSomething(null));
            }
        }

        [ParameterValidatorAspect]
        [MethodInvocationLoggerAspect(typeof(MyLogger))]
        public interface IModuleWithAspects : IModule 
        { 
        }

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public class NotNullAttribute : ParameterValidatorAttribute
        {
            public override void Validate(ParameterInfo param, object value)
            {
                if (value == null) throw new ArgumentNullException(param.Name);
            }
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

        [Test]
        public void DbConnectionProviderTest() 
        {
            const string connString = "Data Source=MSSQL1;Initial Catalog=MyCatalog;Integrated Security=true;";

            Container
                .Provider<IDbConnection, DbConnectionProvider>(Lifetime.Scoped)
                .Factory<IConfiguration>(_ => new ConfigurationBuilder().AddJsonStream
                (
                    new MemoryStream
                    (
                        Encoding.ASCII.GetBytes
                        ($@"
                            {{
                                ""Database"":
                                {{
                                    ""ConnectionString"": ""{connString}"",
                                    ""Provider"": ""SqlServer""
                                }}
                            }}
                        ")
                    )
                ).Build(),
                Lifetime.Singleton);

            using (IInjector injector = Container.CreateInjector()) 
            {
                IDbConnection conn = injector.Get<IDbConnection>();

                Assert.That(conn, Is.InstanceOf<SqlConnection>());
                Assert.That(conn.ConnectionString, Is.EqualTo(connString));
            }
        }

        public class DbConnectionProvider : IServiceProvider
        {
            private class DbOptions
            {
                public string ConnectionString { get; set; }
                public string Provider { get; set; }
            }

            private DbOptions Options { get; } = new DbOptions();

            public DbConnectionProvider(IConfiguration config) =>
                config.GetSection("Database").Bind(Options);

            object IServiceProvider.GetService(Type serviceType)
            {
                if (serviceType != typeof(IDbConnection))
                    throw new NotSupportedException();
#if !LANG_VERSION_8
                IDbConnection connection;
                switch (Options.Provider) 
                {
                    case "SqlServer":
                        connection = new SqlConnection(Options.ConnectionString);
                        break;
                    default:
                        throw new NotSupportedException("Provider not supported");
                }
#else
                IDbConnection connection = Options.Provider switch
                {
                    "SqlServer" => new SqlConnection(Options.ConnectionString),
                    _ => throw new NotSupportedException("Provider not supported")
                };
#endif
                //connection.Open();

                return connection;
            }
        }

        [Test]
        public void LoggerAspectTest()
        {
            Container.Factory(i => new Mock<IModuleWithAspects>().Object);

            using (IInjector injector = Container.CreateInjector())
            {
                IModule module = injector.Get<IModuleWithAspects>();

                Assert.DoesNotThrow(() => module.DoSomething("cica"));
                Assert.That(MyLogger.LastMessage, Is.EqualTo("DoSomething(cica)"));
            }
        }

        public interface ILogger 
        {
            void Write(string msg);
        }

        public class MyLogger : ILogger
        {
            public static string LastMessage { get; private set; }

            public void Write(string msg) => LastMessage = msg;
        }

        public class MethodInvocationLoggerInterceptor<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class 
        {
            private ILogger Logger { get; }

            public MethodInvocationLoggerInterceptor(TInterface target, ILogger logger): base(target) => Logger = logger;

            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
            {
                Logger.Write($"{method.Name}({string.Join(", ", args.Select(arg => arg?.ToString() ?? "null"))})");

                return base.Invoke(method, args, extra);
            }
        }

        [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
        public sealed class MethodInvocationLoggerAspect : AspectAttribute
        {
            public Type Logger { get; }
            
            public MethodInvocationLoggerAspect(Type logger) 
            {
                Kind = AspectKind.Factory;
                Logger = logger;
            }

            public override object GetInterceptor(IInjector injector, Type iface, object instance) => ProxyFactory.Create(
                iface, 
                typeof(MethodInvocationLoggerInterceptor<>).MakeGenericType(iface), 
                new[] 
                { 
                    iface, 
                    typeof(ILogger)
                }, 
                instance, 
                Activator.CreateInstance(Logger));
        }
    }
}
