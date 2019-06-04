# Injector [![Build Status](https://travis-ci.com/Sholtee/injector.svg?branch=master)](https://travis-ci.com/Sholtee/injector)
> A featherweight dependency injector written in C#.

## Overview
[Dependency Injection](https://en.wikipedia.org/wiki/Dependency_injection ) is a design pattern that helps you separate the dependencies of your code from its behavior. Additionaly it makes the code easy to test by let you [mock](https://en.wikipedia.org/wiki/Mock_object ) the dependencies in your unit tests. This libary provides several mechanisms to register, acquire and inherit services. Let's see how!

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
1. [Instantiating the injector](#instantiating-an-injector)
2. [Registering new services](#registering-new-services)
3. [Updating existing services](#updating-existing-services)
4. Locking the injector
5. Requesting services (even parallelly)
6. Destroying the injector (either arbitrarily or automatically)

### Instantiating an injector
Before we'd start we need to instantiate the *root* injector first:
```csharp
using(IInjector injector = Injector.Create())
{
  .
  .
  .
}
```
Why root? Because it has no parent injector which means we are the responsible for freeing (calling the `Dispose()` on) it. Child injectors (created by `injector.CreateChild()` call) can also be disposed arbitrarily, but it's not obligatory because the parent takes care of it:
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
Child injectors inherit their parent's services, but modifying them does not affect their parents. In practice, every application should have only one *root* injector. 

### Registering new services
Registering a service can be done via several patterns (I name them recipes):
- **Service recipe**: The easiest way to register a service, just simply call the `Service()` generic method with the desired lifetime: `injector.Service<IMyService, MyService>(Lifetime.Transient)`. You can also register generic services: `injector.Service(typeof(IMyGenericService<>), typeof(MyGenericService<>), Lifetime.Singleton)`. Later you may request the specialized version of the service without registering it: `injector.Get<IMyGenericService<string>>()` (*instatiating is done on the first request*). A service can request other services via the constructor parameters: `public MyService(IInjector injector, IService_1 dep1, IService_2 dep2){...}` (*implementations must not have more than one public constructor*).
- **Lazy recipe**: Similar to service recipe except that the implementation is unknown in build time (e.g. the service implementation is placed in a module we want to load only if the implementation is requested). So instead of passing the implementation we provide a [resolver](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.ITypeResolver.html ) which will resolve the implementation (NOT a service instance) on the first request: `injector.Lazy<IMyService>(new MyServiceResolver(), Lifetime.Transient)`. Note that we can also register generic services by this method.
- **Factory recipe**: As the name suggests, services registered by this way have a factory function (that is called on the first request): `injector.Factory<IMyService>(thisInjector => new MyService(), Lifetime.Singleton)`. It can be useful e.g. if the service has more than one public constructor. We also can register generic services, in this case the factory function  will be called with the specialized type: `injector.Factory(typeof(IMyGenericService<>), (thisInjector, specializedIMyGenericService) => MyActivator.CreateInstance(typeof(MyGenericService<>), specializedIMyGenericService.GetGenericArguments()))`.
- **Instance recipe**: An instances is a "predefined value" that can act as a service (without lifetime): `injector.Instance<IMyInstance>(myInstance, true)`. *Note that the second parameter instructs the injector to manage the instance lifetime.*

### Updating existing services
In practice, it's useful to separate common functionality (e.g. parameter validation) from the implementation. In this library this can be achieved by [proxy pattern](https://en.wikipedia.org/wiki/Proxy_pattern ). In a brief example:
```csharp
injector
  .Service<IMyModule, MyModule>()
  .Proxy<IMyModule>((thisInjector, typeOfIMyModule, myModuleInstance) => new ParameterValidatorProxy<IMyModule>(myModuleInstance).Proxy);
```
Where the `ParameterValidatorProxy<TInterface>` is an [InterfaceProxy<TInterface>](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.InterfaceProxy-1.html ) containing the parameter validation logic. And that's all. Proxy pattern can be applied against every non-generic service, in any number.

*Note that:*
- Trying to proxy a generic service or an instance (registered via `Instance()` call) will throw an exception.
- Proxying an inherited service WILL NOT affect the parent.

## API Docs
You can find the detailed API docs [here](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.html ).