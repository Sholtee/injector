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
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Solti.Utils.Injector.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to Instantiating generic types are not allowed..
        /// </summary>
        internal static string CANT_INSTANTIATE {
            get {
                return ResourceManager.GetString("CANT_INSTANTIATE", resourceCulture);
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
        ///   Looks up a localized string similar to &quot;{0}&quot; should have exactly one constructor..
        /// </summary>
        internal static string CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED {
            get {
                return ResourceManager.GetString("CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No registered implementation for &quot;{0}&quot;..
        /// </summary>
        internal static string DEPENDENCY_NOT_FOUND {
            get {
                return ResourceManager.GetString("DEPENDENCY_NOT_FOUND", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid dependency type..
        /// </summary>
        internal static string INVALID_DEPENDENCY_TYPE {
            get {
                return ResourceManager.GetString("INVALID_DEPENDENCY_TYPE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The returned object should be instance of {0}..
        /// </summary>
        internal static string INVALID_TYPE {
            get {
                return ResourceManager.GetString("INVALID_TYPE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter must be a class..
        /// </summary>
        internal static string NOT_A_CLASS {
            get {
                return ResourceManager.GetString("NOT_A_CLASS", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Parameter must be an interface..
        /// </summary>
        internal static string NOT_AN_INTERFACE {
            get {
                return ResourceManager.GetString("NOT_AN_INTERFACE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The given interface ({0}) is not assignable from the implementation ({1})..
        /// </summary>
        internal static string NOT_ASSIGNABLE {
            get {
                return ResourceManager.GetString("NOT_ASSIGNABLE", resourceCulture);
            }
        }
    }
}
