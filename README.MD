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
1. [Creating the root container](#)
2. [Registering services](#)
3. [Deriving child containers](#) (optional) 
4. [Decorating inherited services](#) or adding new ones (optional)
5. [Creating an injector](#) It's often referred as "creating a scope"
6. [Requesting service instances](#)
7. Destroying the injector
8. Destroying the root container
Points from 1 to 4 are done in initialization time (typically at application startup), points from 5 to 7 are executed multiple times, parallelly (e.g. per WEB request). Point 8 is done in finalization time (mostly at termination). Note that child containers don't have to be freed since they lifetimes are controlled by the root container.
### Instantiating the injector
Before we'd start we need to instantiate the *root* injector first:
```csharp
using(IInjector injector = Injector.Create()){...}
```
Why root? Because it has no parent injector which means we are responsible for freeing (calling the `Dispose()` on) it. Child injectors (created by `injector.CreateChild()` call) can also be disposed arbitrarily, but it's not mandatory because the parent takes care of it:
```csharp
using(IInjector injector = Injector.Create())
{
  IInjector child_1 = injector.CreateChild();
  Assert.That(child_1.Parent == injector);
  .
  .
  using(IInjector child_2 = injector.CreateChild())
  {
    Assert.That(child_2.Parent == injector);
    .
    .
  } // child_2 released here
} // child_1 released here
```
Remarks:
- Child injectors inherit their parent's services, but modifying them does not affect their parents. 
- Every application should have only one *root* injector. 
### Registering new services
Registering a service can be done via several patterns (I name them recipes):
- **Service recipe**: This is the most common way to file a service.
  To register a simple service just call the `Service()` generic method with the desired interface, implementation and lifetime:
  ```csharp
  injector.Service<IMyService, MyService>(Lifetime.Transient);
  ```
  You can register generic services as well:
  ```csharp
  injector.Service(typeof(IMyGenericService<>), typeof(MyGenericService<>), Lifetime.Singleton);
  ```
  Later you may request the specialized version of the service without registering it:
  ```csharp
  injector.Get<IMyGenericService<string>>();
  ```
  Remarks:
  - Implementations must not have more than one public constructor (or you must annotate the appropriate with the [ServiceActivatorAttribute](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.Annotations.ServiceActivatorAttribute.html ))!
  - A service can request other services via the constructor parameters:
    ```csharp  
    public MyService(IInjector injector, IService_1 dep1, IService_2 dep2){...}
    ```  
  - Instatiating is done on the request (Get() call).
- **Lazy recipe**: Similar to the service recipe except that the implementation is unknown in build time (e.g. the service implementation is placed in a module we want to load only if the implementation is requested). So instead of passing the implementation we provide a [resolver](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.ITypeResolver.html ) which will resolve the implementation (NOT a service instance) on the first request: 
  ```csharp
  injector.Lazy<IMyService>(new MyServiceResolver(), Lifetime.Transient);
  ```
  Note that we can also register generic services via this method.
- **Factory recipe**: As the name suggests services registered by this way have a factory function (that is called on the first request): 
  ```csharp
  injector.Factory<IMyService>(thisInjector => new MyService(), Lifetime.Singleton);
  ```
  It can be useful e.g. if the service has more than one public constructor. In addition we can also register generic services, in this case the factory function will be called with the specialized interface:
  ```csharp
  injector.Factory(typeof(IMyGenericService<>), (thisInjector, specializedIMyGenericService) => MyActivator.CreateInstance(typeof(MyGenericService<>), specializedIMyGenericService.GetGenericArguments()));
  ```
- **Instance recipe**: An instances is a "predefined value" that can act as a service: 
  ```csharp
  injector.Instance<IMyInstance>(myInstance, true);
  ```
  Remarks:
  - The second parameter instructs the injector to dispose the instance when the injector itself is disposed.
  - In contrast of `Singleton`s where each child injector can have its own service instance, values registered by this way are global so child injectors will have the same refernce.
  
*Note that you should not register the injector itself it is done by the system automatically.*
### Updating existing services
In practice, it's useful to separate common functionality (e.g. parameter validation) from the implementation. In this library this can be achieved by [proxy pattern](https://en.wikipedia.org/wiki/Proxy_pattern ). In a brief example:
```csharp
injector
  .Service<IMyModule, MyModule>()
  .Proxy<IMyModule>((thisInjector, myModuleInstance) => new ParameterValidatorProxy<IMyModule>(myModuleInstance));
```
Where the `ParameterValidatorProxy<TInterface>` is an [InterfaceProxy<TInterface>](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.InterfaceProxy-1.html ) containing the parameter validation logic. And that's all. Proxy pattern can be applied against every non-generic service, in any number.
*Note that:*
- Trying to proxy a generic service or an instance (registered via `Instance()` call) will throw an exception.
- Proxying an inherited service WILL NOT affect the parent.
### Locking the injector
To prevent accidental modification of the injector, after configuring you can lock it (by call the prameterless `Lock()` method). After locking any attempt to modify the state of the injector will cause an exception.
It's safe to call the following functions on a locked injector:
- `Get()`
- `Instantiate()`
- `CreateChild()`
- `Dispose()`
  
Note that `Lock()` affects only the instance on which it was called.
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