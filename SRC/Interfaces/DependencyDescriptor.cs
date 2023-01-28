/********************************************************************************
* DependencyDescriptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes a dependency (a property or parameter).
    /// </summary>
    public sealed record DependencyDescriptor
    {
        /// <summary>
        /// Creates a new <see cref="DependencyDescriptor"/> instance.
        /// </summary>
        /// <param name="original">The original member.</param>
        /// <remarks>This class is required as there is no common base class of <see cref="ParameterInfo"/> and <see cref="PropertyInfo"/>.</remarks>
        public DependencyDescriptor(object original)
        {
            switch (original)
            {
                case ParameterInfo parameter:
                    Type = parameter.ParameterType;
                    Name = parameter.Name;
                    break;
                case PropertyInfo property:
                    Type = property.PropertyType;
                    Name = property.Name;
                    break;
                default: throw new NotSupportedException();
            }
            Original= original;
        }

        /// <summary>
        /// The type of the dependency (parameter or property).
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The name of the dependency (parameter or property).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The original member.
        /// </summary>
        public object Original { get; }
    }
}
