/********************************************************************************
* Context.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Internals;

    public partial class InjectorTestsBase<TContainer>
    {
        [Test]
        public void AddingContextualServices_ShouldNotModifyTheParentContainer()
        {
            Container.Service<IInterface_1, Implementation_1_No_Dep>();

            using (IInjector injector = Container.CreateInjector((typeof(IDisposable), null, new Disposable()))) 
            {
                Assert.DoesNotThrow(() => injector.Get<IDisposable>());
                Assert.That(Container.Get<IDisposable>(QueryModes.Default), Is.Null);
            }
        }

    }
}
