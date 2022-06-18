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
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
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
        ///   Looks up a localized string similar to Built in services cannot be overridden..
        /// </summary>
        internal static string BUILT_IN_SERVICE_OVERRIDE {
            get {
                return ResourceManager.GetString("BUILT_IN_SERVICE_OVERRIDE", resourceCulture);
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
        ///   Looks up a localized string similar to &quot;{0}&quot; must have exactly one (annotated) public constructor..
        /// </summary>
        internal static string CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED {
            get {
                return ResourceManager.GetString("CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Injector cannot be invoked when it is about to dispose..
        /// </summary>
        internal static string INJECTOR_IS_BEING_DISPOSED {
            get {
                return ResourceManager.GetString("INJECTOR_IS_BEING_DISPOSED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Aspects should implement the IInterceptorFactory interface..
        /// </summary>
        internal static string INTERCEPTOR_FACTORY_NOT_IMPLEMENTED {
            get {
                return ResourceManager.GetString("INTERCEPTOR_FACTORY_NOT_IMPLEMENTED", resourceCulture);
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
        ///   Looks up a localized string similar to The value of &quot;{0}&quot; cannot be null..
        /// </summary>
        internal static string IS_NULL {
            get {
                return ResourceManager.GetString("IS_NULL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The entry must be built in order to create service instances..
        /// </summary>
        internal static string NOT_BUILT {
            get {
                return ResourceManager.GetString("NOT_BUILT", resourceCulture);
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
        ///   Looks up a localized string similar to Service &quot;{0}&quot; could not be found..
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
        ///   Looks up a localized string similar to The interceptor must have exactly one constructor parameter for the target object..
        /// </summary>
        internal static string TARGET_PARAM_CANNOT_BE_DETERMINED {
            get {
                return ResourceManager.GetString("TARGET_PARAM_CANNOT_BE_DETERMINED", resourceCulture);
            }
        }
    }
}
