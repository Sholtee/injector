﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Solti.Utils.DI.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Solti.Utils.DI.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &quot;{0}&quot; is already registered..
        /// </summary>
        internal static string ALREADY_REGISTERED {
            get {
                return ResourceManager.GetString("ALREADY_REGISTERED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Creating proxy is not allowed here..
        /// </summary>
        internal static string CANT_PROXY {
            get {
                return ResourceManager.GetString("CANT_PROXY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Circular reference: {0}..
        /// </summary>
        internal static string CIRCULAR_REFERENCE {
            get {
                return ResourceManager.GetString("CIRCULAR_REFERENCE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Public constructor with the given layout can not be found..
        /// </summary>
        internal static string CONSTRUCTOR_NOT_FOUND {
            get {
                return ResourceManager.GetString("CONSTRUCTOR_NOT_FOUND", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &quot;{0}&quot; must have exactly one (annotated) public constructor..
        /// </summary>
        internal static string CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED {
            get {
                return ResourceManager.GetString("CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Entry can not be specialized..
        /// </summary>
        internal static string ENTRY_CANNOT_BE_SPECIALIZED {
            get {
                return ResourceManager.GetString("ENTRY_CANNOT_BE_SPECIALIZED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Inappropriate ownership..
        /// </summary>
        internal static string INAPPROPRIATE_OWNERSHIP {
            get {
                return ResourceManager.GetString("INAPPROPRIATE_OWNERSHIP", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The injector can not hold more than {0} transient services. It usually indicates that you were recycling the injector..
        /// </summary>
        internal static string INJECTOR_SHOULD_BE_RELEASED {
            get {
                return ResourceManager.GetString("INJECTOR_SHOULD_BE_RELEASED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to All constructor arguments must be an interface/Lazy&lt;interface&gt;..
        /// </summary>
        internal static string INVALID_CONSTRUCTOR {
            get {
                return ResourceManager.GetString("INVALID_CONSTRUCTOR", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Non interface/Lazy&lt;interface&gt; arguments must be specified as an explicit argument..
        /// </summary>
        internal static string INVALID_CONSTRUCTOR_ARGUMENT {
            get {
                return ResourceManager.GetString("INVALID_CONSTRUCTOR_ARGUMENT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Injector must not contain abstract service entries..
        /// </summary>
        internal static string INVALID_INJECTOR_ENTRY {
            get {
                return ResourceManager.GetString("INVALID_INJECTOR_ENTRY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The object must be an instance of &quot;{0}&quot;..
        /// </summary>
        internal static string INVALID_INSTANCE {
            get {
                return ResourceManager.GetString("INVALID_INSTANCE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value of &quot;{0}&quot; can not be null..
        /// </summary>
        internal static string IS_NULL {
            get {
                return ResourceManager.GetString("IS_NULL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The matching strategy could not be determined..
        /// </summary>
        internal static string NO_STRATEGY {
            get {
                return ResourceManager.GetString("NO_STRATEGY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The servicereference belongs to an another entry..
        /// </summary>
        internal static string NOT_BELONGING_REFERENCE {
            get {
                return ResourceManager.GetString("NOT_BELONGING_REFERENCE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The given values are not equal..
        /// </summary>
        internal static string NOT_EQUAL {
            get {
                return ResourceManager.GetString("NOT_EQUAL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value of &quot;{0}&quot; should be null..
        /// </summary>
        internal static string NOT_NULL {
            get {
                return ResourceManager.GetString("NOT_NULL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The service has no factory function..
        /// </summary>
        internal static string NOT_PRODUCIBLE {
            get {
                return ResourceManager.GetString("NOT_PRODUCIBLE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter should not be abstract..
        /// </summary>
        internal static string PARAMETER_IS_ABSTRACT {
            get {
                return ResourceManager.GetString("PARAMETER_IS_ABSTRACT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter should not be generic..
        /// </summary>
        internal static string PARAMETER_IS_GENERIC {
            get {
                return ResourceManager.GetString("PARAMETER_IS_GENERIC", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter must be a class..
        /// </summary>
        internal static string PARAMETER_NOT_A_CLASS {
            get {
                return ResourceManager.GetString("PARAMETER_NOT_A_CLASS", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter must be an interface..
        /// </summary>
        internal static string PARAMETER_NOT_AN_INTERFACE {
            get {
                return ResourceManager.GetString("PARAMETER_NOT_AN_INTERFACE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No registered implementation for &quot;{0}&quot;..
        /// </summary>
        internal static string SERVICE_NOT_FOUND {
            get {
                return ResourceManager.GetString("SERVICE_NOT_FOUND", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attempt to request a dependency that should live shorter than the requestor should..
        /// </summary>
        internal static string STRICT_DI {
            get {
                return ResourceManager.GetString("STRICT_DI", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown lifetime: {0}..
        /// </summary>
        internal static string UNKNOWN_LIFETIME {
            get {
                return ResourceManager.GetString("UNKNOWN_LIFETIME", resourceCulture);
            }
        }
    }
}