# Injector.NET [![Build status](https://ci.appveyor.com/api/projects/status/2fej57bdmyij4v08?svg=true)](https://ci.appveyor.com/project/Sholtee/injector) [![NuGet Badge](https://buildstats.info/nuget/injector.net)](https://www.nuget.org/packages/injector.net/) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/injector/badge.svg?branch=master)](https://coveralls.io/github/Sholtee/injector?branch=master)
> A featherweight dependency injector & proxy generator written in C#.

**This documentation is OUTDATED, new version coming soon**
## Overview
[Dependency Injection](https://en.wikipedia.org/wiki/Dependency_injection ) is a design pattern that helps you separate the dependencies of your code from its behavior. Additionaly it makes the code easy to test by let you [mock](https://en.wikipedia.org/wiki/Mock_object ) the dependencies in your unit tests. This library provides several mechanisms to register, acquire and inherit services.
## Key features
- Dependency injection via [constructor parameters](https://en.wikipedia.org/wiki/Dependency_injection#Constructor_injection )
- [Lazy dependency resolution](https://www.quickdevnotes.com/lazy-initialization/ )
- Dynamic [proxy](https://en.wikipedia.org/wiki/Proxy_pattern ) generator
- Custom service decoration (using proxies)
- [Duck typing](https://en.wikipedia.org/wiki/Duck_typing ) support (using proxies)
- Container inheritance
- All common service lifecycles are supported: `Singleton`, `Transient`, `Scoped`, `Instance`
- Configurable constructor selection (using attributes)
- "Auto" service registration (using attributes)
- Extensible
- and many more...
## About services in general
- They are interfaces (including the injector itself).
- They are hosted in containers.
- Every service can be requested multiple times but can be registered only once.
- Producible services are instantiated only when they are requested.
- Every producible service has its own lifetime, which can be:
  * `Singleton`: Instantiated only once (per declaring container) on the first request and released automatically when the declaring container is disposed.
  * `Scoped`: Instantiated only once (per injector) on the first request and released automatically when the injector is disposed.
  * `Transient`: Instantiated on every request and the caller is responsible for freeing the requested services.
## Workflow of DI
1. [Creating the root container](#creating-the-root-container)
2. [Registering services](#registering-services)
3. [Deriving child containers](#deriving-child-containers) (optional) 
4. [Decorating inherited services](#decorating-services) or adding new ones (optional)
5. [Creating an injector](#) It's often referred as "creating a scope"
6. [Requesting service instances](#)
7. Destroying the injector
8. Destroying the root container

Points from 1 to 4 are done in initialization time (typically at application startup), points from 5 to 7 are executed multiple times, parallelly (e.g. per WEB request). Point 8 is done in finalization time (mostly at termination). Note that child containers don't have to be freed since they lifetimes are controlled by the root container.
### Creating the root container
The first step before we'd start is creating a *root* container:
```csharp
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
  container.Get<IMyGenericService<string>>(QueryMode.AllowSpecialization);
  ```
  Remarks:
  - Implementations must not have more than one public constructor (or you must annotate the appropriate one with the `ServiceActivatorAttribute`)!
  - A service may request other services via the constructor parameters:
    ```csharp 
    public class MyService: IMyService
    {
      public MyService(IInjector injector, IService_1 dep1, IService_2 dep2) {...}
    }
    ```
- **Lazy recipe**: Similar to the service recipe except that the implementation is unknown in build time (e.g. the service implementation is placed in a module we want to load only if the service is requested). So instead of passing the implementation we provide an `ITypeResolver` which will resolve the implementation: 
  ```csharp
  container.Lazy<IMyService>(new LazyTypeResolver<IMyService>("Absolute.Path.To.Assembly", "Implementation.Fullname"), Lifetime.Transient);
  ```
  Remarks: 
  - You may also register generic lazy services by the same way.
  - In most of cases the built in `LazyTypeResolver` is suitable.
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
- **Instance recipe**: Instances are "predefined values" that can act as a service: 
  ```csharp
  container.Instance<IMyService>(service, true);
  ```
  The second parameter instructs the injector to dispose the instance when the injector itself is released (defaults to false).
- **Abstract recipe**: Defines an abstract service that must be overridden in the derived container(s).
  ```csharp
  container.Abstract<IMyService>();
  ```
  Remarks: 
  - You can override an abstract entry by using any of the recepies described above.
  - You can not create injector from a container having abstract entries.
  
*Note that you should not register the injector itself it is done by the system automatically.*
### Decorating services
In practice, it's useful to separate common functionality (e.g. parameter validation) from the implementation. In this library this can be achieved by [proxy pattern](https://en.wikipedia.org/wiki/Proxy_pattern ). In a brief example:
```csharp
container
  .Service<IMyModule, MyModule>()
  .Proxy<IMyModule>((injector, serviceInstance) => new ParameterValidatorProxy<IMyModule>(serviceInstance));
```
Where the `ParameterValidatorProxy<TInterface>` is an `InterfaceInterceptor<TInterface>` descendant containing the parameter validation logic.

*Remarks:*
- Proxy pattern can be applied in any number against a service.
- Applying proxies is done on the service request.
- Trying to decorate a non producible servce (generic, instance, abstract) will throw.
- Decorating an inherited service WILL NOT affect the parent.
### Requesting services
After finishing the configuration you can request services via the `Get()` method:
```csharp
IMyService svc = injector.Get<IMyService>();
``` 
Keep in mind the followings:
- Calling `Get()` is a thread safe operation so you may share the configured injector between different threads.
- Requesting an unregistered or an open generic service will cause an exception.
- Requesting services as a constructor parameter is more convenient than using the `Get()` method.
- The caller should free the requested service if it is an `IDisposable` and has the `Transient` lifetime. To determine it's lifetime call the `QueryServiceInfo()` method.

Alternatively you can use the `Instantiate()` function (since version *0.0.3*). This method takes a `Type` (non-abstract class) you want to instantate, resolves its dependencies (via constructor parameters) and creates a new instance from it. There are two major differences compared to `Get()`:
- The instantiated `Type` does not have to be a registered service.
- You can pass arbitrary arguments to the constructor via the `explicitArgs` parameter (which means the instantiated type can have non-registered/non-interface dependency).
### Resolving contextual dependencies
Sometimes the service being requested is contextual (for instance resolving the `HttpRequest` that triggered the current session). In this case you have two options: deriving a new container for the current session or use the events provided by the injector (recommended):
```csharp
injector.OnServiceRequest += (sender, arg) =>
{
  // Note that there was no service registered with this interface
  // so without this the injector.Get() call would throw.
  if (arg.Interface == typeof(IHttpRequest))
  {
    // Define a value to be returned.  
    arg.Service = ...;
  }
}
``` 
Note that as we learned the requested service always must be an interface but the `HttpRequest` implements nothing. That's the time when the [duck typing](https://en.wikipedia.org/wiki/Duck_typing ) comes into view:
```csharp
// Custom interface covering the necessary members from the `HttpRequest`
public interface IHttpRequest
{
  string this[string key] { get; }
}
...
injector.OnServiceRequest += (sender, arg) =>
{
  if (arg.Interface == typeof(IHttpRequest))
  {  
    arg.Service = Request
	  .Act()
	  .Like<IHttpRequest>();
  }
}
``` 
## Auto config the injector
Starting from version *0.0.2* you can use the `ServiceAttribute` on service classes to annotate them for auto config. Auto config is done by a simple call to the `injector.Setup(sourceAssembly)` which instructs the system to seek and register all the annotated services from the `sourceAssembly`.
## Resources
You can browse the [detailed API docs](https://sholtee.github.io/injector ) or the [benchmark results](https://sholtee.github.io/injector/perf/ ).
## Supported frameworks
This project currently targets *.NET Core 2.X* only.
## Version history
- 0.0.1: Initial release
- 0.0.2:
  - *added* `IServiceInfo.Factory` property
  - *added* `IInjectorExtensions.Proxy()` overload
  - *added* [ServiceAttribute](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.Annotations.ServiceAttribute.html ) and [ConfigFromAssembly](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.ConfigFromAssembly.html ) classes
  - *added* [ServiceActivatorAttribute](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.Annotations.ServiceActivatorAttribute.html ) class
  - *fixed* multiple type resolver invocation issue
  - *fixed* `IInjectorExtensions.Instance()` should not dispose the instance by default
  - some code optimization
- 0.0.3
  - *added* `IInjector.Instantiate()` method