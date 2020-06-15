# Injector.NET [![Build status](https://ci.appveyor.com/api/projects/status/2fej57bdmyij4v08?svg=true)](https://ci.appveyor.com/project/Sholtee/injector) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/injector) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/injector/badge.svg?branch=master)](https://coveralls.io/github/Sholtee/injector?branch=master) [![NuGet Badge](https://buildstats.info/nuget/injector.net)](https://www.nuget.org/packages/injector.net/)
> A featherweight dependency injector written in C#.

**This documentation refers the version 3.X of the library**
## Overview
[Dependency Injection](https://en.wikipedia.org/wiki/Dependency_injection ) is a design pattern that helps you separate the dependencies of your code from its behavior. Additionaly it makes the code easy to test by let you [mock](https://en.wikipedia.org/wiki/Mock_object ) the dependencies in your unit tests. This library provides several mechanisms to register, acquire and inherit services.
## Key features
- Dependency injection via [constructor parameters](https://en.wikipedia.org/wiki/Dependency_injection#Constructor_injection )
- [Lazy dependency resolution](https://www.quickdevnotes.com/lazy-initialization/ )
- Custom service decoration (using [AOP](https://en.wikipedia.org/wiki/Aspect-oriented_programming ) or proxies)
- [Duck typing](https://en.wikipedia.org/wiki/Duck_typing ) (using proxies)
- Generic service support
- Container inheritance
- All well known service lifecycles are supported: `Singleton`, `Transient`, `Scoped`, `Instance`
- Configurable constructor selection (using attributes)
- "Auto" service registration (using attributes)
- Extensible
- and many more...
## About services in general
- They are interfaces.
- They are hosted in containers.
- They are provided by injectors.
- Requested services should be registered.
- Every service can be requested multiple times.
- Producible services are instantiated only when they are requested.
- Every producible service has its own lifetime, which can be:
  * `Singleton`:
    - Instantiated only once on the first request and released automatically when the declaring container is disposed (container controlled lifetime). 
	- Dependency resolution is also done from the declaring container.
  * `Scoped`:
    - Instantiated only once (per injector) on the first request and released automatically when the parent injector is disposed (injector controlled lifetime).
	- Dependency resolution is done from the parent injector.
  * `Transient`:
    - Instantiated on every request and released automatically when the parent injector is disposed (injector controlled lifetime).
    - Dependency resolution is done from the parent injector.
	
  As you can see you should never free producible services manually.
- Since `Singleton` services can be accessed parallelly they (and their dependencies) have to be thread safe.
## Workflow of DI
1. [Creating the root container](#creating-the-root-container)
2. [Registering services](#registering-services)
3. [Decorating services](#decorating-services) (optional)
4. [Deriving child containers](#deriving-child-containers) (optional)
   1. Overriding abstract services or registering new ones
   2. Decorating services
   3. Deriving child containers
5. [Creating injector](#creating-an-injector) from a container. It's often referred as "creating a scope"
6. [Requesting service instances](#requesting-services)
7. Destroying the injector
8. Destroying the root container

Points from 1 to 4 are done in initialization time (typically at application startup), points from 5 to 7 are executed multiple times, parallelly (e.g. per WEB request). Point 8 is done in finalization time (mostly at termination). Note that child containers don't have to be freed since their lifetimes are controlled by their parents.
### Creating the root container
The first step before we'd start is creating a *root* container:
```csharp
using Solti.Utils.DI.Interfaces;
using Solti.Utils.DI;

using(IServiceContainer container = new ServiceContainer()){...}
```
In most of cases you should declare only one *root* container although you can have as much as you want.
### Deriving child containers
Container inheritance is one of the key features of this library. It lets you define a base container and derive children that inherit services from it.
```csharp
using(IServiceContainer root = new ServiceContainer())
{
  ...
  IServiceContainer child = root.CreateChild(); // There is no need for disposing it manually	
  ...
}
```
Remarks:
- Modifying child containers does not affect their parents.
- You may declare *abstract* services that must be overridden in the derived container.
### Registering services
Registering a service can be done via several patterns (I name them recipes):
- **Service recipe**: This is the most common way to file a service.
  To register a simple service just call the `Service()` generic method with the desired interface, implementation and lifetime:
  ```csharp
  container.Service<IMyService, MyService>(Lifetime.Transient);
  ```
  You can register generic services as well:
  ```csharp
  container.Service(typeof(IMyGenericService<>), typeof(MyGenericService<>), Lifetime.Singleton);
  ```
  Later you may request the specialized version of the service entry without registering it:
  ```csharp
  container.Get<IMyGenericService<string>>(QueryModes.AllowSpecialization);
  ```
  Remarks:
  - Implementations must not have more than one public constructor (or you must annotate the appropriate one with the `ServiceActivatorAttribute`)!
  - A service may request other services via the constructor parameters:
    - ```csharp 
      public class MyService: IMyService
      {
        public MyService(IInjector injector, IService_1 dep1, IService_2 dep2) {...}
      }
      ```
    - Deferred resolution also possible:
	  ```csharp 
      public class MyService: IMyService
      {
        public MyService(Lazy<IService_1> dep1, Lazy<IService_2> dep2) {...}
      }
      ```
    - You can mark the requested dependency as optional. In this case the system won't throw if the dependency can not be found:
      ```csharp 
      public class MyService: IMyService
      {
        public MyService(IService_1 dep1, [Options(Optional = true)]IService_2 dep2) {...}
      }
      ```   
- **Factory recipe**: As the name suggests services registered by this way have a factory function: 
  ```csharp
  container.Factory<IMyService>(injector => new MyService(), Lifetime.Singleton);
  ```
  It can be useful e.g. if the service has more than one public constructor. In case of generic services the factory function will be called with the specialized interface:
  ```csharp
  container.Factory(typeof(IMyGenericService<>), (injector, serviceInterface) => 
  {
    Assert.That(serviceInterface.IsGenericTypeDefinition, Is.False);
	Assert.That(serviceInterface.GetGenericTypeDefinition(), Is.EqualTo(typeof(IMyGenericService<>)));
    ...  
  });
  ```
- **Provider recipe**: Providers are factory services with well defined layout (see the [IServiceProvider](https://docs.microsoft.com/en-us/dotnet/api/system.iserviceprovider?view=netcore-3.1 ) interface) and dependecies:
  ```csharp
  using System;
  using System.Data;
  using System.Data.SqlClient;
  using Microsoft.Extensions.Configuration;
  ...
  public class DbConnectionProvider: IServiceProvider
  {
    private class DbOptions
    {
      public string ConnectionString {get; set;}
      public string Provider {get; set;} 
    }

    private DbOptions Options {get;} = new DbOptions();

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
      connection.Open();

      return connection;	  
    }
  }
  ...
  container
    .Factory<IConfiguration>(
		_ => new ConfigurationBuilder().AddJsonFile(
  #if DEV
          "config.dev.json"
  #else
          "config.live.json"
  #endif
		).Build(),
		Lifetime.Singleton)
    .Provider<IDbConnection, DbConnectionProvider>(Lifetime.Scoped);
  ```
- **Instance recipe**: Instances are "predefined values" that can act as a service: 
  ```csharp
  container.Instance<IMyService>(service, releaseOnDispose: true);
  ```
  The second parameter instructs the container to dispose the instance when the container itself is released (defaults to false).
- **Abstract recipe**: Defines an abstract service that must be overridden in the derived container(s).
  ```csharp
  container.Abstract<IMyService>();
  ```
  Remarks: 
  - You can override an abstract entry by using any of the recepies described above.
  - You can not create injector from a container having abstract entries.
 
Remarks: 
- You may also register more service with the same interface by naming them:
  ```csharp
  container.Service<IMyService, MyServiceImeplementation_1>("svc1", Lifetime.Transient);
  container.Factory<IMyService>("svc2", i => ..., Lifetime.Singleton);
  ...
  ```
  Later you can request them via constructor parameters as well:
  ```csharp
  class MyOtherService: IMyOtherService
  {
    public MyOtherService([Options(Name = "svc2")]IMyService dep) {...}
	...
  }  
  ```
- You should not register the injector itself it is done by the system automatically.
- A container should be treated as "sealed" if the `CreateChild()` or `CreateInjector()` method was called on it.
### Decorating services
In practice, it's useful to separate common functionality (e.g. parameter validation) from the implementation. In this library this can be achieved by [proxy pattern](https://en.wikipedia.org/wiki/Proxy_pattern ). In a brief example:
```csharp
using Solti.Utils.DI.Interfaces;
using Solti.Utils.DI;
using Solti.Utils.Proxy; 
...
container
  .Service<IMyModule, MyModule>()
  .Proxy<IMyModule>((injector, serviceInstance) => ProxyFactory.Create<IMyModule, ParameterValidatorProxy<IMyModule>>(target: serviceInstance));
  
// There is a shortcut to the line above:
// container.Proxy<IMyModule, ParameterValidatorProxy<IMyModule>>();
```
Where the `ParameterValidatorProxy<TInterface>` is an `InterfaceInterceptor<TInterface>` descendant containing the parameter validation logic:
```csharp
using Solti.Utils.Proxy; 
...
// Base class of all the validator attributes
public abstract class ParameterValidatorAttribute: Attribute
{
  public abstract void Validate(ParameterInfo param, object value);
}
// Sample validator
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class NotNullAttribute : ParameterValidatorAttribute
{
  public override void Validate(ParameterInfo param, object value)
  {
    if (value == null) throw new ArgumentNullException(param.Name);
  }
}
...
public class ParameterValidatorProxy<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
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
```
*Remarks:*
- Proxy pattern can be applied in any number against a service.
- Applying proxies is done on the service request.
- Trying to decorate a non producible service (generic, instance, abstract) will throw.
- Decorating an inherited service WILL NOT affect the parent.
- `ProxyFactory` is an individual component so you can use it anywhere in your code.
- Proxies may also have dependencies (`ProxyFactory` supports this scenario).
- You can setup transactions using proxies as well:
  ```csharp
  using Solti.Utils.DI.Interfaces;
  using Solti.Utils.DI;
  using Solti.Utils.Proxy;
  ...
  public class MyModuleUsingDbConnection : IMyModuleUsingDbConnection
  {
    // Connection is not necessarily used -> Lazy
    private readonly Lazy<IDbConnection> FConnection;
    public IDbConnection Connection => FConnection.Value;
	
    public MyModuleUsingDbConnection(Lazy<IDbConnection> dbConn)
    {
      Connection = dbConn;
    }
	
    void IMyModuleUsingDbConnection.DoSomething() {...}
  }
  ...
  public class TransactionManager<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
  {
    private readonly Lazy<IDbConnection> FConnection;
    public IDbConnection Connection => FConnection.Value;
	
    public TransactionManager(TInterface target, Lazy<IDbConnection> dbConn) : base(target)
    {
      Connection = dbConn;
    }
	
    public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
    {
      // Create transactions only if needed
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
  ...
  Container
    .Provider<IDbConnection, DbConnectionProvider>(Lifetime.Scoped)
    .Service<IMyModuleUsingDbConnection, MyModuleUsingDbConnection>(Lifetime.Scoped)
    .Proxy<IMyModuleUsingDbConnection, TransactionManager<IMyModuleUsingDbConnection>>();
  ```
- The `TransactionManager` and `ParameterValidatorProxy` proxy is available as an extension in the [Injector.NET.Extensions](https://www.nuget.org/packages/Injector.NET.Extensions/ ) package.
### Aspects
Decorating services can be done via attributes as well. In this case we declare an attribute (derived from `AspectAttribute`) that tells the injector which interceptors should be used. Doing this we introduce the [AOP](https://en.wikipedia.org/wiki/Aspect-oriented_programming ) in our code:
```csharp
...
// Define an aspect for ParameterValidatorProxy (see above)
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public sealed class ParameterValidatorAspect : AspectAttribute
{
  public override Type GetInterceptor(Type iface) => typeof(ParameterValidatorProxy<>).MakeGenericType(iface);
}
...
// Then annotate the desired interface(s):
[ParameterValidatorAspect]
public interface IService
{
  void DoSomething([NotNull] object arg); 
}
```
Notes:
- As you can see there is no need for registering the `ParameterValidatorProxy<>` manually.
- Applying aspects is done in the order you use them:
  ```csharp
  // On service invocations the system first validates the user then the method parameters.
  [UserValidatorAspect, ParameterValidatorAspect]
  public interface IMyService {...}
  ```
- Among other aspects you can find this one in the [Injector.NET.Extensions](https://www.nuget.org/packages/Injector.NET.Extensions/ ) package.

You can create parameterized aspects as well:
```csharp
// Define the abstract logger
public interface ILogger 
{
  void Write(string msg);
}

// Define a concrete logger
public class ConsoleLogger : ILogger
{
  public void Write(string msg) => Console.WriteLine(msg);
}

// Define the interceptor
public class MethodInvocationLoggerInterceptor<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class 
{
  public ILogger Logger { get; }

  public MethodInvocationLoggerInterceptor(TInterface target, ILogger logger): base(target) => Logger = logger;

  public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
  {
    Logger.Write($"{method.Name}({string.Join(", ", args.Select(arg => arg?.ToString() ?? "null"))})");
    return base.Invoke(method, args, extra);
  }
}

// Define the aspect
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
...
// Use the aspect
[MethodInvocationLoggerAspect(typeof(ConsoleLogger))]
public interface IService
{
  ...
}
```
### Auto config
There are some attributes to annotate services for "auto config" e.g.:
- ```csharp
  [Service(typeof(IMyService), Lifetime.Scoped)] // It will use the "service recipe"
  public MyService: IMyService {...}
  ```

After annotating the desired types you just have to simply call the [container.Setup(sourceAssembly)](https://sholtee.github.io/injector/doc/Solti.Utils.DI.IServiceContainerExtensions.html#Solti_Utils_DI_IServiceContainerExtensions_Setup_Solti_Utils_DI_IServiceContainer_Assembly_System_String_ ) (extension) method which instructs the system to seek and register all the annotated services from the `sourceAssembly`.
### Creating an injector
```csharp
using (IInjector injector = container.CreateInjector())
{
  ...
}
```
or
```csharp
await using (IInjector injector = container.CreateInjector())
{
  ...
}
```
*Remarks:*
- Injector instances are not thread safe so every session / worker must have its own injector.
- For proper resource management, at the end of the session you should dispose the injector (though the parent container disposal would release it as well).
### Requesting services
After finishing the configuration you can request services via the `Get()` method:
```csharp
IMyService svc = injector.Get<IMyService>();
```
or
```csharp
IMyService svc = injector.Get<IMyService>("servicename");
```
*Remarks:*
- Requesting an unregistered or an open generic service will cause an exception. If your dependency is optional you can use the `injector.TryGet()` method.
- Requesting services as a constructor parameter is more convenient than using the `injector.Get()` method.
- You may request the closed pair of an open generic service without registering it:
  ```csharp
  container.Service(typeof(IMyGenericService<>), ...);
  ...
  using(IInjector injector = container.CreateInjector())
  {  
    IMyGenericService<string> scv = injector.Get<IMyGenericService<string>>();
  }
  ```
- To access all services with the given interface you can use the magic `IEnumerable<>` service:
  ```csharp
  // Return all the IMyService instances regardless their names
  IEnumerable<IMyService> svcs = injector.Get<IEnumerable<IMyService>>();
  // Service instantiation is done during enumeration
  ```

Alternatively you can use the `injector.Instantiate()` function that takes a `Type` (non-abstract class) you want to instantate, resolves its dependencies (via constructor parameters) and creates a new instance from it: 
```csharp
container.Service<IMyService, MyService>();
...
public class OuterService
{
  // Parameter names must not be obfuscated.
  public OuterService(IMyService dep, int num){...}
}
...
using(IInjector injector = container.CreateInjector())
{  
  OuterService instance = injector.Instantiate<OuterService>(new Dictionary<string, object>
  {
    {"num", 10}
  });
} // Since dependencies may be disposed here invoking the instantiated object is safe in the using block only
```
As you can see there are three major differences compared to `injector.Get()`:
- The instantiated `Type` does not have to be a registered service.
- You can pass arbitrary arguments to the constructor via the `explicitArgs` parameter (which means the instantiated type may have non-registered/non-interface dependency).
- The caller is responsible for freeing the returned object (that must be done before the injector disposal).
### Resolving contextual dependencies
Sometimes the service being requested is contextual (for instance the `HttpRequest` that triggered the current session). To solve this issue we can extend the `UnderlyingContainer` that serves the current injector:
```csharp
using Solti.Utils.DI.Interfaces;
using Solti.Utils.DI;
...
using (IInjector injector = Container.CreateInjector()) 
{
  injector
    .UnderlyingContainer
    .Instance(HttpRequest); // will throw if `HttpRequest` is not an interface 
  ...
}
``` 
As we learned the requested service always must be an interface but the [HttpRequest](https://docs.microsoft.com/en-us/dotnet/api/system.web.httprequest?view=netframework-4.8&viewFallbackFrom=netcore-3.1 ) implements nothing. That's the time when the [duck typing](https://en.wikipedia.org/wiki/Duck_typing ) comes into view:
```csharp
using Solti.Utils.DI.Interfaces;
using Solti.Utils.DI;
using Solti.Utils.Proxy;
...
// Custom interface covering the necessary members from the `HttpRequest`
public interface IHttpRequest
{
  string this[string key] { get; }
}
...
using (IInjector injector = Container.CreateInjector()) 
{
  injector
    .UnderlyingContainer
    .Instance(HttpRequest.Act().Like<IHttpRequest>());
  ...
}
```
### Inline dependencies
A service can request its owner `IInjector` as a regular dependency (via constructor parameter). Doing this makes it possible to get services anywhere in the implementation (by invoking the `IInjector.Get()` method). This is we call *inline dependencies*.

Notes:
- The [dependency list](https://sholtee.github.io/injector/doc/Solti.Utils.DI.Interfaces.IServiceReference.html#Solti_Utils_DI_Interfaces_IServiceReference_Dependencies ) will not contain services requested outside of the constructor.
- Disposing inline dependencies is done by the system as well.
## Strict DI
Consider the following registration:
```csharp
...
container
  .Service<IMyService, MyService>(Lifetime.Transient)
  .Service<IMyOtherService, MyOtherServiceDependsOnIMyService>(Lifetime.Singleton);
...
using (IInjector injector = container.CreateInjector())
{
  var svc = injector.Get<IMyOtherService>();
  ...
}
```
Leaving the `using` block the `IMyService` instance requested by `MyOtherServiceDependsOnIMyService` won't be released because the holder service still alives (until the declaring container is disposed). This situation is called "captive dependency". To avoid it you have two options:
- You can declare your consuming service with a `Lifetime` less than or equal to the `Lifetime` of consumed service.
- You can enable *Strict DI* (disabled by default) in `Solti.Utils.DI.config.json` which will instruct the injector to throw if a captive dependency can be found.
## MS preferred DI
Microsoft also defines its own [interface](https://docs.microsoft.com/en-us/dotnet/api/system.iserviceprovider?view=netcore-3.1 ) for dependency resolution. This library has built in support for it:
```csharp
using System;
...
using (container.CreateProvider(out IServiceProvider provider))
{
  var svc = provider.GetService<IMyService>();
}
```
or
```csharp
await using (container.CreateProvider(out IServiceProvider provider))
{
  ...
}
```
Differences compared to `IInjector`:
- Every requested service is optional (`IServiceProvider.GetService()` does not throw if a service can not be found). This rule applies for services requested via constructor parameters as well.
- You can request named services only by using `OptionsAttribute` (`IServiceProvider.GetService()` has no name parameter).
- Since the `IServiceProvider` interface is not an `IDisposable` descendant you should release the disposable returned by the `CreateProvider()` to end the scope lifetime.
## Resources
You can browse the [detailed API docs](https://sholtee.github.io/injector ) or the [benchmark results](https://sholtee.github.io/injector/perf/ ).
## Supported frameworks
This project currently targets *.NET Standard* 2.0 and 2.1.
## Migrating from version 1.X
- All the attributes have been moved to the `Solti.Utils.DI` namespace so you just have to remove the `Solti.Utils.DI.Annotations` usings.
- `Lazy` recipe has completely been removed. To preserve this functionality you can implement your own deferring logic in a `Factory` function.
- *.NET Standard* 1.6 support has been dropped. Since modern .NET projects should not target the v1.X branch you might not be affected by this.
- Other breaking changes were done on API's you should not call in everyday use.
## Migrating from version 2.X
- You should add +1 using (`Solti.Utils.DI.Interfaces`) in files where you use the [general interfaces](https://sholtee.github.io/injector/doc/Solti.Utils.DI.Interfaces.html ).
- There is NO need for referencing the `Solti.Utils.DI.Interfaces` assembly directly.
## Version history
Can be found [here](https://github.com/Sholtee/injector/blob/master/history.md )