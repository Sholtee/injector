﻿/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Encapsulates a service and its dependencies into a reference counted container.
    /// </summary>
    public sealed class ServiceReference: DisposeByRefObject
    {
        #region ReferenceList
        private sealed class ReferenceList : ICollection<ServiceReference>
        {
            private readonly List<ServiceReference> FUnderlyingList = new List<ServiceReference>(); 

            public int Count => FUnderlyingList.Count;

            public bool IsReadOnly => false;

            public void Add(ServiceReference item)
            {
                FUnderlyingList.Add(item);
                item.AddRef();
            }

            public void Clear()
            {
                FUnderlyingList.ForEach(@ref => @ref.Release());
                FUnderlyingList.Clear();
            }

            public bool Contains(ServiceReference item) => FUnderlyingList.Contains(item);

            public void CopyTo(ServiceReference[] array, int arrayIndex) => throw new NotImplementedException();

            public IEnumerator<ServiceReference> GetEnumerator() => FUnderlyingList.GetEnumerator();

            public bool Remove(ServiceReference item)
            {
                if (FUnderlyingList.Remove(item)) 
                {
                    item.Release();
                    return true;
                }
                return false;
            }

            IEnumerator IEnumerable.GetEnumerator() => FUnderlyingList.GetEnumerator();
        }
        #endregion

        /// <summary>
        /// Creates a new <see cref="ServiceReference"/> instance.
        /// </summary>
        public ServiceReference() => Dependencies = new ReferenceList();

        /// <summary>
        /// The referenced service instance.
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// The dependencies of this service <see cref="Instance"/>.
        /// </summary>
        public ICollection<ServiceReference> Dependencies { get; }

        /// <summary>
        /// Disposes the referenced service <see cref="Instance"/> and decrements the reference counter of all the <see cref="Dependencies"/>.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Elso helyen szerepeljen h a fuggosegeket a Dispose()-ban meg hasznalni tudja a szerviz peldany.
                //

                (Instance as IDisposable)?.Dispose();

                //
                // Ha a GC-nek kell felszabaditania a ServiceReference peldanyt akkor vmit nagyon elkurtunk
                // es mar semmi ertelme a referencia szamlalasnak -> ezert a lenti sor csak "disposeManaged"
                // eseten ertelmezett.
                //

                Dependencies.Clear();
            }

            base.Dispose(disposeManaged);
        }
    }
}