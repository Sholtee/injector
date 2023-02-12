/********************************************************************************
* AspectAttribute.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

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
    ///     public object Invoke(IInvocationContext context, InvokeInterceptorDelegate callNext)
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
    ///         return callNext();
    ///     }
    /// }
    /// ...
    /// // Define the comcrete aspect
    /// [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    /// public sealed class ParameterValidatorAspect : AspectAttribute
    /// {
    ///     public override Type UnderlyingInterceptor { get; } = typeof(ParameterValidatorProxy);
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
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public abstract class AspectAttribute : Attribute, IAspect
    {
        /// <summary>
        /// The underlying interceptor.
        /// </summary>
        /// <remarks>This type must be a (instantiable) class implementing the <see cref="IInterfaceInterceptor"/> interface.</remarks>
        public abstract Type UnderlyingInterceptor { get; }
    }
}
