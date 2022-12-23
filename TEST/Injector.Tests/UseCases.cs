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
using System.Linq.Expressions;
using System.Text;

using Microsoft.Extensions.Configuration;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.UseCases
{
    using Interfaces;
    using Primitives;
    using Proxy;
    using Proxy.Generators;

    [TestFixture]
    public class UseCases
    {
        public interface IModule
        {
            void DoSomething(object arg);
            void DoSomethingElse();
        }

        public interface IMyModule1 : IModule { }
        public interface IMyModule2 : IModule { }

        public class Module1 : IMyModule1 
        {
            public void DoSomething(object arg) { }
            public void DoSomethingElse() { }
        }

        public class Module2 : IMyModule2 
        {
            public void DoSomething(object arg) { }
            public void DoSomethingElse() { }
        }

        [MethodInvocationLoggerAspect(typeof(MyLogger))]
        public interface IModuleWithAspects : IModule 
        { 
        }

        [Test]
        public void DbConnectionProviderTest() 
        {
            const string connString = "Data Source=MSSQL1;Initial Catalog=MyCatalog;Integrated Security=true;";

            using IScopeFactory root = ScopeFactory.Create(svcs => svcs
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
                Lifetime.Singleton));

            using IInjector injector = root.CreateScope();

            IDbConnection conn = injector.Get<IDbConnection>();

            Assert.That(conn, Is.InstanceOf<SqlConnection>());
            Assert.That(conn.ConnectionString, Is.EqualTo(connString));
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

                IDbConnection connection = Options.Provider switch
                {
                    "SqlServer" => new SqlConnection(Options.ConnectionString),
                    _ => throw new NotSupportedException("Provider not supported")
                };

                //connection.Open();

                return connection;
            }
        }

        [Test]
        public void LoggerAspectTest()
        {
            using IScopeFactory root = ScopeFactory.Create(svcs => svcs.Factory(i => new Mock<IModuleWithAspects>().Object, Lifetime.Transient));

            IInjector injector = root.CreateScope();

            IModule module = injector.Get<IModuleWithAspects>();

            Assert.DoesNotThrow(() => module.DoSomething("cica"));
            Assert.That(MyLogger.LastMessage, Is.EqualTo("DoSomething(cica)"));
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

            public override object Invoke(InvocationContext context)
            {
                Logger.Write($"{context.InterfaceMethod.Name}({string.Join(", ", context.Args.Select(arg => arg?.ToString() ?? "null"))})");

                return base.Invoke(context);
            }
        }

        [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
        public sealed class MethodInvocationLoggerAspect : AspectAttribute, IInterceptorFactory<Expression<ApplyProxyDelegate>>
        {
            public Type Logger { get; }
            
            public MethodInvocationLoggerAspect(Type logger) 
            {
                Logger = logger;
            }

            public Expression<ApplyProxyDelegate> GetInterceptor(Type iface)
            {
                StaticMethod ctor = new ProxyGenerator
                (
                    iface,
                    typeof(MethodInvocationLoggerInterceptor<>).MakeGenericType(iface)
                )
                .GetGeneratedType()
                .GetConstructor(new[] { iface, typeof(ILogger) })
                .ToStaticDelegate();

                return (injector, type, target) => ctor(target, Activator.CreateInstance(Logger));
            }
        }
    }
}
