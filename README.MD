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
1. Instantiating the injector
2. Update existing services (if the injector is a child)
3. Registering new services
4. Locking the injector (WIP feature yet)
5. Requesting services (even parallelly)
6. Destroying the injector (either arbitrarily or automatically)

## Instantiating an injector
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
Child injectors inherit their parent's services, but modifying a child does not affect its parent. In practice, every application should have only one *root* injector. 

## API Docs
[Here](http://htmlpreview.github.io/?https://github.com/Sholtee/injector/blob/master/DOC/Solti.Utils.DI.html )