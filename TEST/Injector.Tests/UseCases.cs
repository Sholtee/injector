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

        [ParameterValidatorAspect]
        public class Module3UsingAspect : IModule
        {
            public void DoSomething([NotNull] object arg)
            {             
            }

            public void DoSomethingElse()
            {
            }
        }

        [MethodInvocationLoggerAspect(typeof(MyLogger))]
        public interface IModuleUsingAspect : IModule 
        { 
        }

        [Test]
        public void DbConnectionProviderTest() 
        {
            const string connString = "Data Source=MSSQL1;Initial Catalog=MyCatalog;Integrated Security=true;";

            using IScopeFactory root = ScopeFactory.Create(static svcs => svcs
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
            using IScopeFactory root = ScopeFactory.Create(svcs => svcs.Factory(i => new Mock<IModuleUsingAspect>().Object, Lifetime.Transient));

            IInjector injector = root.CreateScope();

            IModule module = injector.Get<IModuleUsingAspect>();

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

        public sealed class MethodInvocationLoggerInterceptor<TLogger> : IInterfaceInterceptor where TLogger : ILogger, new()
        {
            private ILogger Logger { get; } = new TLogger();

            public object Invoke(IInvocationContext context, Next<object> callNext)
            {
                Logger.Write($"{context.InterfaceMethod.Name}({string.Join(", ", context.Args.Select(arg => arg?.ToString() ?? "null"))})");

                return callNext();
            }
        }

        [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
        public sealed class MethodInvocationLoggerAspect : AspectAttribute
        {
            public Type Logger { get; }

            public override Type UnderlyingInterceptor => typeof(MethodInvocationLoggerInterceptor<>).MakeGenericType(Logger);

            public MethodInvocationLoggerAspect(Type logger) 
            {
                Logger = logger;
            }
        }

        [Test]
        public void ParameterValidatorAspectTest()
        {
            using IScopeFactory root = ScopeFactory.Create(svcs => svcs.Service<IModule, Module3UsingAspect>(Lifetime.Transient));

            IInjector injector = root.CreateScope();

            IModule module = injector.Get<IModule>();

            Assert.DoesNotThrow(() => module.DoSomething("cica"));
            Assert.Throws<ArgumentNullException>(() => module.DoSomething(null));
        }

        public abstract class ParameterValidatorAttribute : Attribute
        {
            public abstract void Validate(ParameterInfo param, object value);
        }

        // Sample validator
        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public sealed class NotNullAttribute : ParameterValidatorAttribute
        {
            public override void Validate(ParameterInfo param, object value)
            {
                if (value is null)
                    throw new ArgumentNullException(param.Name);
            }
        }

        public sealed class ParameterValidatorProxy : IInterfaceInterceptor
        {
            public object Invoke(IInvocationContext context, Next<object> callNext)
            {
                foreach (var descr in context.TargetMethod.GetParameters().Select(
                  (p, i) => new
                  {
                      Parameter = p,
                      Value = context.Args[i],
                      Validators = p.GetCustomAttributes<ParameterValidatorAttribute>()
                  }))
                {
                    foreach (ParameterValidatorAttribute validator in descr.Validators)
                    {
                        validator.Validate(descr.Parameter, descr.Value);
                    }
                }

                return callNext();
            }
        }

        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
        public sealed class ParameterValidatorAspect : AspectAttribute
        {
            public override Type UnderlyingInterceptor { get; } = typeof(ParameterValidatorProxy);
        }
    }
}
