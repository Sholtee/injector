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
    /// <remarks>This class is required as there is no common base class for <see cref="ParameterInfo"/> and <see cref="PropertyInfo"/>.</remarks>
    public sealed record DependencyDescriptor
    {
        /// <summary>
        /// Creates a new <see cref="DependencyDescriptor"/> instance.
        /// </summary>
        /// <param name="original">The original member which should be either a <see cref="ParameterInfo"/> or a <see cref="PropertyInfo"/> instance.</param>
        public DependencyDescriptor(object original)
        {
            switch (original)
            {
                case ParameterInfo parameter:
                    Type = parameter.ParameterType;
                    Name = parameter.Name;
                    Options = parameter.GetCustomAttribute<OptionsAttribute>();
                    break;
                case PropertyInfo property:
                    Type = property.PropertyType;
                    Name = property.Name;
                    Options = property.GetCustomAttribute<OptionsAttribute>();
                    break;
                default: throw new NotSupportedException();
            }
            Original= original;
        }

        /// <summary>
        /// The type of dependency (parameter or property).
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The name of dependency (parameter or property).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Bound options.
        /// </summary>
        public OptionsAttribute? Options { get; }

        /// <summary>
        /// The original member (which is either a <see cref="PropertyInfo"/> or a <see cref="ParameterInfo"/>).
        /// </summary>
        public object Original { get; }
    }
}
