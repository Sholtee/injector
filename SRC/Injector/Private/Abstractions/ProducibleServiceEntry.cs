/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal abstract class ProducibleServiceEntry : AbstractServiceEntry, ISupportsSpecialization, ISupportsProxying
    {
        #region Protected
        protected ProducibleServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory) : base(@interface, name, null)
        {
            Factory = Ensure.Parameter.IsNotNull(factory, nameof(factory));
            this.ApplyAspects();
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation) : base(@interface, name, implementation)
        {
            //
            // Ancestor does the rest of validation
            //

            Ensure.Parameter.IsNotNull(implementation, nameof(implementation));

            if (!@interface.IsGenericTypeDefinition)
            {
                Factory = ServiceActivator.Get(implementation);
                this.ApplyAspects();
            }
            else
                //
                // Just to validate the implementation.
                //

                implementation.GetApplicableConstructor();
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs) : base(@interface, name, implementation)
        {
            //
            // Ancestor does the rest of validation
            //

            Ensure.Parameter.IsNotNull(implementation, nameof(implementation));
            Ensure.Parameter.IsNotNull(explicitArgs, nameof(explicitArgs));

            if (!@interface.IsGenericTypeDefinition)
            {
                if (explicitArgs is IReadOnlyDictionary<string, object?> dict)
                {
                    Func<IInjector, IReadOnlyDictionary<string, object?>, object> factoryEx = ServiceActivator.GetExtended(implementation);

                    Factory = (injector, _) => factoryEx(injector, dict);
                }
                else
                {
                    Func<IInjector, object, object> factoryEx = ServiceActivator.GetExtended(implementation, explicitArgs.GetType());

                    Factory = (injector, _) => factoryEx(injector, explicitArgs);
                }

                this.ApplyAspects();
            }
            else
                //
                // Just to validate the implementation.
                //

                implementation.GetApplicableConstructor();

            ExplicitArgs = explicitArgs;
        }
        #endregion

        public override object CreateInstance(IInjector scope, out object? lifetime)
        {
            object result = (Factory ?? throw new InvalidOperationException(Resources.NOT_PRODUCIBLE))(scope, Interface);

            //
            // Getting here indicates that the service graph validated successfully.
            //

            Flags |= ServiceEntryFlags.Validated;
            lifetime = result as IDisposable;
            return result;
        }

        public object? ExplicitArgs { get; }

        public abstract AbstractServiceEntry Specialize(params Type[] genericArguments);

        Func<IInjector, Type, object>? ISupportsProxying.Factory
        {
            get => Factory;
            set => Factory = value;
        }
    }
}