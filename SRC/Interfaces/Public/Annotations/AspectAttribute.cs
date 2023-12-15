/********************************************************************************
* AspectAttribute.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines an abstract aspect that can be applied against service interfaces or classes. Aspects are the prefered way to decorate service instances.
    /// <code>
    /// // Base class of all the validator attributes
    /// public abstract class ParameterValidatorAttribute : Attribute
    /// {
    ///     public abstract void Validate(ParameterInfo param, object value);
    /// }
    /// ...
    /// // Sample validator
    /// [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    /// public class NotNullAttribute : ParameterValidatorAttribute
    /// {
    ///     public override void Validate(ParameterInfo param, object value)
    ///     {
    ///         if (value is null)
    ///             throw new ArgumentNullException(param.Name);
    ///     }
    /// }
    /// ...
    /// public class ParameterValidatorProxy : IInterfaceInterceptor
    /// {
    ///     public ParameterValidator(IDependency dependency) {...}
    ///     
    ///     public object? Invoke(IInvocationContext context, CallNextDelegate&lt;IInvocationContext?, object&gt; callNext)
    ///     {
    ///         foreach (var descr in context.TargetMethod.GetParameters().Select
    ///         (
    ///             (p, i) => new
    ///             {
    ///                 Parameter = p,
    ///                 Value = context.Args[i],
    ///                 Validators = p.GetCustomAttributes&lt;ParameterValidatorAttribute&gt;()
    ///             }
    ///         ))
    ///         {
    ///             foreach (var validator in descr.Validators)
    ///             {
    ///                 validator.Validate(descr.Parameter, descr.Value);
    ///             }
    ///         }
    ///         return callNext(context);
    ///     }
    /// }
    /// ...
    /// // Define the comcrete aspect
    /// [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    /// public sealed class ParameterValidatorAspect : AspectAttribute
    /// {
    ///     public ParameterValidatorAspect(): base(typeof(ParameterValidatorProxy)) { }
    /// }
    /// // Then annotate the desired interface ...
    /// [ParameterValidatorAspect]
    /// public interface IService
    /// {
    /// void DoSomething([NotNull] object arg);
    /// }
    /// 
    /// // ... OR class (recommended)
    /// 
    /// [ParameterValidatorAspect]
    /// public class Service : IService
    /// {
    ///     // Only methods implementing the above declared interface can be annoteted 
    ///     void DoSomething([NotNull] object arg) { ...}
    /// }
    /// </code>
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public class AspectAttribute : Attribute
    {
        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        public AspectAttribute(Type interceptor)
            => Interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));

        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        public AspectAttribute(Type interceptor, object explicitArgs): this(interceptor)
            => ExplicitArgs = explicitArgs ?? throw new ArgumentNullException(nameof(explicitArgs));

        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        public AspectAttribute(Expression<CreateInterceptorDelegate> factory)
            => Factory = factory ?? throw new ArgumentNullException(nameof(factory));

        /// <summary>
        /// Factory function responsible for creating the concrete interceptor. 
        /// </summary>
        public Expression<CreateInterceptorDelegate>? Factory { get; }

        /// <summary>
        /// The underlying interceptor.
        /// </summary>
        /// <remarks>This type must be a class that can be instantiated and implements the <see cref="IInterfaceInterceptor"/> interface.</remarks>
        public Type? Interceptor { get; }

        /// <summary>
        /// Explicit arguments to be passed:
        /// <code>new {ctorParamName1 = ..., ctorParamName2 = ...}</code>
        /// </summary>
        public object? ExplicitArgs { get; }
    }
}
