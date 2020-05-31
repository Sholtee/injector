/********************************************************************************
* ParameterValidatorAttribute.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.DI.Extensions.Aspects
{
    /// <summary>
    /// Defines an abstract parameter validator.
    /// </summary>
    public abstract class ParameterValidatorAttribute : Attribute
    {
        /// <summary>
        /// The method containing the validation logic.
        /// </summary>
        public abstract void Validate(ParameterInfo param, object value);
    }
}
