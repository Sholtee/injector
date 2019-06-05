﻿/********************************************************************************
* ParameterIsAttribute.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class ParameterIsAttribute: Attribute
    {
        private static readonly ConcurrentDictionary<Type, IParameterValidator> FCache = new ConcurrentDictionary<Type, IParameterValidator>();

        public IReadOnlyList<IParameterValidator> Validators { get; }

        public ParameterIsAttribute(params Type[] validators)
        {
            Validators = validators.Select(validator => FCache.GetOrAdd
            (
                validator,
                @void => (IParameterValidator) validator.CreateInstance(new Type[0])
            )).ToArray();
        }
    }
}
