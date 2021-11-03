# Injector.NET [![Build status](https://ci.appveyor.com/api/projects/status/2fej57bdmyij4v08?svg=true)](https://ci.appveyor.com/project/Sholtee/injector/branch/master) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/injector/master) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/injector/badge.svg?branch=master)](https://coveralls.io/github/Sholtee/injector?branch=master) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/sholtee/injector/master)
> A featherweight dependency injector written in C#.

|Package name|NuGet link|
|:--:|:--:|
|**Injector.NET**|[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/injector.net)](https://www.nuget.org/packages/injector.net )|
|**Injector.NET.Interfaces**|[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/injector.net.interfaces)](https://www.nuget.org/packages/injector.net.interfaces )|

**This documentation refers the version 6.X of the library**
## Overview
[Dependency Injection](https://en.wikipedia.org/wiki/Dependency_injection ) is a design pattern that helps you separate the dependencies of your code from its behavior. Additionaly it makes the code easy to test by let you [mock](https://en.wikipedia.org/wiki/Mock_object ) the dependencies in your unit tests.
## Key features
- Dependency injection via [constructor parameters](https://en.wikipedia.org/wiki/Dependency_injection#Constructor_injection ) or [setters](https://en.wikipedia.org/wiki/Dependency_injection#Setter_injection )
- [Lazy dependency resolution](https://www.quickdevnotes.com/lazy-initialization/ )
- Custom service decoration (using [AOP](https://en.wikipedia.org/wiki/Aspect-oriented_programming ) or proxies)
- [Duck typing](https://en.wikipedia.org/wiki/Duck_typing ) (using proxies)
- Generic service support
- All well known service lifecycles are supported: `Singleton`, `Transient`, `Scoped`, `Pooled`, `Instance`
- Configurable constructor selection (using attributes)
- Extensible
- and many more...
## About services in general
- They are interfaces.
- They are declared in a `IServiceCollection`.
- They are provided by an `IInjector`.
- Every service can be requested multiple times.
- Producible services are instantiated only when they are requested.
- Every producible service has its own lifetime, which can be:
  * `Singleton`:
    - Instantiated only once in the _root scope_ (on the first request) and released automatically when the root is disposed. 
    - Dependency resolution is also done from the _root scope_.
  * `Scoped`:
    - Instantiated only once per _parent scope_ (on the first request) and released automatically when the parent is disposed.
	- Dependency resolution is done from the parent scope.
  * `Transient`:
    - Instantiated on every request and released automatically when the _parent scope_ is disposed.
    - Dependency resolution is done from the parent scope.
  * `Pooled`:
    - Instantiated in a separate [pool](https://en.wikipedia.org/wiki/Object_pool_pattern ) (if necessary) and released automatically when the _root scope_ is disposed
	- Dependency resolution is done from a dedicated scope.
	
  As you can see you should never free producible services manually.
- Since `Singleton` and `Instance` services may be accessed parallelly they (and their dependencies) have to be thread safe.
## Workflow of DI
1. [Creating a scope factory](#creating-a-scope-factory)
2. [Registering services](#registering-services)
3. [Decorating services](#decorating-services) (optional)
4. [Creating a scope](#creating-a-scope)
5. [Requesting service instances](#requesting-services)
6. Destroying the injector
7. Destroying the root scope

Points from 1 to 3 are done in initialization time (typically at application startup), points from 4 to 6 are executed multiple times, parallelly (e.g. per WEB request). Point 7 is done in finalization time (mostly at termination).
### Creating a scope factory
The first step before we'd start is creating a _scope factory_:
```csharp
using Solti.Utils.DI.Interfaces;
using Solti.Utils.DI;

using(IScopeFactory scopeFactory = ScopeFactory.Create(svcs => /*registering services*/))
{
  ...
}
```
In most of the cases you should use only one *factory* although you can have as much as you want.
### Registering services
Registering a service may be done via several patterns (I name them recipes):
- **Service recipe**: This is the most common way to file a service.
  To register a simple service just call the `Service()` generic method with the desired interface, implementation and lifetime:
  ```csharp
  svcs.Service<IMyService, MyService>(Lifetime.Transient);
  ```
  You can register generic services as well:
  ```csharp
  svcs.Service(typeof(IMyGenericService<>), typeof(MyGenericService<>), Lifetime.Singleton);
  ```
  Remarks:
  - Implementations must not have more than one public constructor (or you must annotate the appropriate one with the `ServiceActivatorAttribute`)!
  - A service may request other services via the constructor parameters or properties:
    - ```csharp 
      public class MyService: IMyService
      {
        public MyService(IInjector injector, IService_1 dep1, IService_2 dep2) {...}
        [Inject]
        publiuc IService3 Dep3 {get; set; /*or init*/}
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
  - By default, services must not have non-interface dependency. To work around this limitation you can provide custom constructor arguments:
    ```csharp
    svcs.Service<IMyService, MyService>(new Dictionary<string, object?>{["paramName"] = someValue}, Lifetime.Transient);
    ```
- **Factory recipe**: As the name suggests services registered by this way have a factory function: 
  ```csharp
  svcs.Factory<IMyService>
  (
    injector => new MyService(injector, injector.Get<IService_1>(), injector.Get<IService_2>())
    {
      Dep3 = injector.Get<IService_3>()
    },
    Lifetime.Singleton
  );
  ```
  It can be useful e.g. if the service has more than one public constructor. In case of generic services the factory function will be called with the specialized interface:
  ```csharp
  svcs.Factory(typeof(IMyGenericService<>), (injector, serviceInterface) => 
  {
    Assert.That(serviceInterface.IsGenericTypeDefinition, Is.False);
	Assert.That(serviceInterface.GetGenericTypeDefinition(), Is.EqualTo(typeof(IMyGenericService<>)));
    ...  
  });
  ```
- **Provider recipe**: Providers are factory services with well-defined layout (see the [IServiceProvider](https://docs.microsoft.com/en-us/dotnet/api/system.iserviceprovider?view=netcore-3.1 ) interface) and dependencies:
  ```csharp
  using System;

  using ServiceStack.Data;
  using ServiceStack.OrmLite;

  namespace Services
  {
    using API;

    public class MySqlDbConnectionFactoryProvider : IServiceProvider
    {
        public IConfig Config { get; }

        public MySqlDbConnectionFactoryProvider(IConfig config) => Config = config ?? throw new ArgumentNullException(nameof(config));

        public object GetService(Type serviceType)
        {
            if (serviceType != typeof(IDbConnectionFactory))
                throw new NotSupportedException();

            return new OrmLiteConnectionFactory(Config.ConnectionString, MySqlDialect.Provider)
            {
                AutoDisposeConnection = true
            };
        }
    }
  }
  ...
  svcs.Provider<IDbConnectionFactory, MySqlDbConnectionFactoryProvider>(Lifetime.Singleton);
  ```
- **Instance recipe**: Instances are "predefined values" that can act as a service: 
  ```csharp
  svcs.Instance<IMyService>(service);
  ```
  Instances are NEVER disposed by the system, you have to do it manually.

Remarks: 
- You may also register more service with the same interface by naming them:
  ```csharp
  svcs.Service<IMyService, MyServiceImeplementation_1>("svc1", Lifetime.Transient);
  svcs.Factory<IMyService>("svc2", i => ..., Lifetime.Singleton);
  ...
  ```
  Later you can request them individually:
  ```csharp
  class MyOtherService: IMyOtherService
  {
    public MyOtherService([Options(Name = "svc2")]IMyService dep) {...}
    ...
  }  
  ```
  or in a _batched_ form with the magic `IEnumerable<>` service:
  ```csharp
  class MyOtherService: IMyOtherService
  {
    public MyOtherService(IEnumerable<IMyService> deps)
    {
      Assert.That(deps.Count(), Is.EqualTo(2));
      ...
    }
    ...
  }  
  ```
- You should not register the injector itself it is done by the system automatically.
### Decorating services
In practice, it's useful to separate common functionality (e.g. parameter validation) from the implementation. In this library this can be achieved by [proxy pattern](https://en.wikipedia.org/wiki/Proxy_pattern ). In a brief example:
```csharp
using Solti.Utils.DI.Interfaces;
using Solti.Utils.DI;
using Solti.Utils.Proxy; 
...
svcs
  .Service<IMyModule, MyModule>(Lifetime.Scoped)
  .Proxy<IMyModule, ParameterValidatorProxy<IMyModule>>();
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
    if (value is null)
      throw new ArgumentNullException(param.Name);
  }
}
...
public class ParameterValidatorProxy<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
{
  public ParameterValidator(TInterface target) : base(target)
  { 
  }

  public override object Invoke(InvocationContext context)
  {
    foreach(var descr in context.Method.GetParameters().Select(
      (p, i) => new 
      { 
        Parameter = p, 
        Value = context.Args[i], 
        Validators = p.GetCustomAttributes<ParameterValidatorAttribute>() 
      }))
    {
      foreach (var validator in descr.Validators) 
      {
        validator.Validate(descr.Parameter, descr.Value);
      }
    }

    return base.Invoke(context);
  }
}
```
*Remarks:*
- Proxy pattern can be applied in any number against a service.
- Applying proxies is done on the service request.
- Trying to decorate a non producible service (generic, instance) will throw.
- `ProxyFactory` is an individual component, so you can use it anywhere in your code.
- Proxies may also have dependencies (`ProxyFactory` supports this scenario).
- The underlying functionality is provided by the [ProxyGen.NET](https://github.com/Sholtee/proxygen ) library
### Aspects
Decorating services can be done by attributes as well. In this case we declare an attribute (derived from the `AspectAttribute`) that instructs the system which interceptors should be used. Doing so we introduce the [AOP](https://en.wikipedia.org/wiki/Aspect-oriented_programming ) in our code:
```csharp
...
// Define an aspect for the ParameterValidatorProxy (see above)
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
### Naked aspects
An aspect is naked when it references its interceptor by name (and the interceptor is placed in a separate assembly). This practice ensures the _separation of concerns_ (the project containing the service interface won't reference logic related assemblies implicitly).
```csharp
...
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public class NakedValidatorAspectAttribute: AspectAttribute 
{
  public override Type GetInterceptor(Type iface)
  {
    Type interceptor = Type.GetType("Solti.Utils.DI.Extensions.Aspects.ParameterValidator`1, Solti.Utils.DI.Extensions, Version=3.3.1.0, Culture=neutral, PublicKeyToken=null", throwOnError: true);
    return interceptor.MakeGenericType(iface);
  }
}
```
### Creating a scope
```csharp
using (IInjector injector = scopeFactory.CreateScope())
{
  ...
}
```
or
```csharp
await using (IInjector injector = scopeFactory.CreateScope())
{
  ...
}
```
*Remarks:*
- `IInjector` instances are NOT thread safe so every session / worker must have its own scope.
- To release resources held by the scope, at the end of the session you should dispose the injector.
### Requesting services
```csharp
IMyService svc = injector.Get<IMyService>();
```
or
```csharp
IMyService svc = injector.Get<IMyService>("servicename");
```
*Remarks:*
- Requesting an unregistered or an open generic service will throw by default. If your dependency is optional, use the `injector.TryGet()` method.
- Requesting services as a constructor parameter is more convenient than using the `injector.Get()` method.
- You may request the closed pair of an open generic service without registering it:
  ```csharp
  using(IScopeFactory scopeFactory = ScopeFactory.Create(svcs => svcs.Service(typeof(IMyGenericService<>), ...)))
  {
    ...
    using(IInjector injector = scopeFactory.CreateScope())
    {  
      IMyGenericService<string> svc = injector.Get<IMyGenericService<string>>();
    }
  }
  ```
- To access all services with the given interface, use the magic `IEnumerable<>` service:
  ```csharp
  // Return all the IMyService instances regardless their names
  IEnumerable<IMyService> svcs = injector.Get<IEnumerable<IMyService>>();
  // Service instantiation is done during enumeration
  ```

Alternatively you can use the `injector.Instantiate()` function that takes a `Type` (non-abstract class) you want to instantiate, resolves its dependencies (via constructor parameters) and creates a new instance from it: 
```csharp
public class OuterService
{
  // Parameter names must not be obfuscated.
  public OuterService(IMyService dep, int num){...}
}
...
using(IScopeFactory scopeFactory = ScopeFactory.Create(svcs => svcs.Service<IMyService, MyService>(Lifetime.Scoped)))
{
  using(IInjector injector = scopeFactory.CreateScope())
  {  
    OuterService instance = injector.Instantiate<OuterService>(new Dictionary<string, object>
    {
      {"num", 10}
    });
  } // Using the instantiated object is safe in the using block only
}
```
As you can see there are three major differences compared to `injector.Get()`:
- The instantiated `Type` does not have to be a registered service.
- You can pass arbitrary arguments to the constructor via the `explicitArgs` parameter (which means the instantiated type may have non-registered/non-interface dependency).
- The caller is responsible for freeing the returned object (that must be done before the scope disposal).
### Inline dependencies
A service can request its owner `IInjector` as a regular dependency (via constructor parameter). Doing this makes it possible to get services anywhere in the implementation (by invoking the `IInjector.Get()` method). This is we call *inline dependencies*.

Notes:
- Disposing inline dependencies is done by the system as well.
## Strict DI
Consider the following registration:
```csharp
...
svcs
  .Service<IMyService, MyService>(Lifetime.Transient)
  .Service<IMyOtherService, MyOtherServiceDependsOnIMyService>(Lifetime.Singleton);
...
using (IInjector injector = scopeFactory.CreateScope())
{
  var svc = injector.Get<IMyOtherService>();
  ...
}
```
Leaving the `using` block the `IMyService` instance requested by `MyOtherServiceDependsOnIMyService` won't be released because the requester still alives (until the root scope is disposed). This situation is called _captive dependency_. To avoid it you have two options:
- Declare your consuming service with a `Lifetime` less than or equal to the `Lifetime` of consumed service.
- Enable _Strict DI_ validation (disabled by default):
  ```csharp
  using(IScopeFactory scopeFactory = ScopeFactory.Create(svcs => ..., new ScopeOptions {StrictDI = true}))
  {
    ...
  }
  ```
## MS preferred DI
Microsoft also defines its own [interface](https://docs.microsoft.com/en-us/dotnet/api/system.iserviceprovider?view=netcore-3.1 ) for dependency resolution. This library has built in support for it:
```csharp
using System;

using Solti.Utils.DI;
using Solti.utils.DI.Interfaces;

...
using(IScopeFactory scopeFactory = ScopeFactory.Create(svcs => ..., new ScopeOptions {SupportsServiceProvider = true}))
{
  ...
  using(scopeFactory.CreateScope(out IServiceProvider sp))
  {
  }
}
```
Differences compared to `IInjector`:
- Every requested service is optional (so `IServiceProvider.GetService()` does not throw if a service can not be found). This rule applies to services requested via constructor parameters too.
- You can request named services only by using the `OptionsAttribute` (`IServiceProvider.GetService()` has no name parameter).
- Since the `IServiceProvider` interface is not an `IDisposable` descendant you should release the disposable returned by the `CreateScope()` to end the scope lifetime.
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
## Migrating from version 3.X
- Auto service registration has been removed, so you should register all the services manually
- Configuration has been moved to `runtimeconfig.json` (and got a new layout). See [this](https://github.com/Sholtee/injector/blob/master/TEST/Injector.Tests/runtimeconfig.template.json ) as a reference
## Migrating from version 4.X
- *Custom converter* support has been dropped so instances returned by *factory function*s must implement the service interface.
- Renamed built in service `IServiceGraph` to `IServicePath`.
## Version history
Can be found [here](https://github.com/Sholtee/injector/blob/master/history.md )