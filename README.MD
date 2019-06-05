# Injector [![Build Status](https://travis-ci.com/Sholtee/injector.svg?branch=master)](https://travis-ci.com/Sholtee/injector)
> A featherweight dependency injector written in C#.
## Overview
[Dependency Injection](https://en.wikipedia.org/wiki/Dependency_injection ) is a design pattern that helps you separate the dependencies of your code from its behavior. Additionaly it makes the code easy to test by let you [mock](https://en.wikipedia.org/wiki/Mock_object ) the dependencies in your unit tests. This library provides several mechanisms to register, acquire and inherit services. Let's see how!
## About services in general
- They are interfaces (including the injector itself).
- They are hosted in an injector.
- Every service can be requested multiple times but can be registered only once.
- Services are instantiated only when they are requested.
- Every service has its own lifetime, which can be:
  * `Singleton`: Instantiated only once on the first request and released automatically when the containing injector is released.
  * `Transient`: Instantiated on every request and the caller is responsible for freeing the requested services.
## Injector lifetime
The lifetime of an injector is the following:
1. [Instantiating the injector](#instantiating-the-injector)
2. [Registering new services](#registering-new-services)
3. [Updating existing services](#updating-existing-services)
4. [Locking the injector](#locking-the-injector)
5. [Requesting services](#requesting-services) (even parallelly)
6. Destroying the injector (either arbitrarily or automatically), see below
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
  - Implementations must not have more than one public constructor!
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
  The second parameter instructs the injector to dispose the instance when the injector itself is disposed.
  
*Note that you should not register the injector itself it is done by the system automatically.*
### Updating existing services
In practice, it's useful to separate common functionality (e.g. parameter validation) from the implementation. In this library this can be achieved by [proxy pattern](https://en.wikipedia.org/wiki/Proxy_pattern ). In a brief example:
```csharp
injector
  .Service<IMyModule, MyModule>()
  .Proxy<IMyModule>((thisInjector, myModuleInstance) => new ParameterValidatorProxy<IMyModule>(myModuleInstance).Proxy);
```
Where the `ParameterValidatorProxy<TInterface>` is an [InterfaceProxy<TInterface>](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.InterfaceProxy-1.html ) containing the parameter validation logic. And that's all. Proxy pattern can be applied against every non-generic service, in any number.
*Note that:*
- Trying to proxy a generic service or an instance (registered via `Instance()` call) will throw an exception.
- Proxying an inherited service WILL NOT affect the parent.
### Locking the injector
To prevent accidental modification of the injector, after configuring you can lock it (by call the prameterless `Lock()` method). After locking any attempt to modify the state of the injector will cause an exception.
It's safe to call the following functions on a locked injector:
- `Get()`
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
- Requesting an unregistered or and open generic service will cause an exception.
- Requesting services as a constructor parameter is more convenient than using the `Get()` method.
- The caller should free the requested service if it is an `IDisposable` and has the `Transient` lifetime. To determine it's lifetime call the `QueryServiceInfo()` method.
## API Docs
You can find the detailed API docs [here](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.html ).