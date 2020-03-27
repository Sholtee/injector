/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Implements the <see cref="IComposite{T}"/> interface.
    /// </summary>
    /// <typeparam name="TInterface">The interface on which we want to apply the composite pattern.</typeparam>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Derived types can access these methods via the Children property")]
    public abstract class Composite<TInterface> : Disposable, ICollection<TInterface>, IComposite<TInterface> where TInterface : class, IComposite<TInterface>
    {
        private readonly HashSet<TInterface> FChildren = new HashSet<TInterface>();

        //
        // A gyermekekkel kapcsolatos metodusok (lasd "Children" property) lehetnek hivva parhuzamosan.
        //

        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim();

        private TInterface? FParent;

        private TInterface Self { get; }

        #region Protected
        /// <summary>
        /// Creates a new <see cref="Composite{TInterface}"/> instance.
        /// </summary>
        /// <param name="parent">The (optional) parent entity. It can be null.</param>
        protected Composite(TInterface? parent)
        {
            Self = this as TInterface ?? throw new Exception(string.Format(Resources.Culture, Resources.INTERFACE_NOT_SUPPORTED, typeof(TInterface)));

            parent?.Children.Add(Self);
        }

        /// <summary>
        /// Disposal logic related to this class.
        /// </summary>
        /// <param name="disposeManaged">Check out the <see cref="Disposable"/> class.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Kivesszuk magunkat a szulo gyerekei kozul (kiveve ha gyoker elemunk van, ott nincs szulo).
                //

                FParent?.Children.Remove(Self);

                //
                // ToArray() azert kell h iteracio kozben is eltavolithassunk elemet a listabol. 
                // A gyermek listanak elmeletileg itt mar nem szabadna sok elemet tartalmaznia 
                // (ha minden injector megfeleloen fel volt szabaditva).
                //

                foreach (IDisposable child in FChildren.ToArray())
                {
                    child.Dispose();
                }

                Assert(!FChildren.Any());

                FLock.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Disposal logic related to this class.
        /// </summary>
        protected override async ValueTask AsyncDispose()
        {
            FParent?.Children.Remove(Self);

            foreach (IAsyncDisposable child in FChildren.ToArray())
            {
                await child.DisposeAsync();
            }

            Assert(!FChildren.Any());

            FLock.Dispose();
        }
        #endregion

        #region IComposite
        /// <summary>
        /// The parent of this entity. Can be null.
        /// </summary>
        public TInterface? Parent
        {
            get => FParent;

            [MethodImpl(MethodImplOptions.NoInlining)]
            set
            {
                var sf = new StackFrame(skipFrames: 1, fNeedFileInfo: false);
                if (sf.GetMethod().GetCustomAttribute<CanSetParentAttribute>() == null)
                    throw new InvalidOperationException(Resources.CANT_SET_PARENT);

                FParent = value;
            }
        }

        /// <summary>
        /// The children of this entity.
        /// </summary>
        public virtual ICollection<TInterface> Children
        {
            get
            {
                Ensure.NotDisposed(this);

                return this;
            }
        }
        #endregion

        #region ICollection
        //
        // Csak a lenti tagoknak kell szalbiztosnak lenniuk.
        //

        int ICollection<TInterface>.Count 
        {
            get 
            {
                Ensure.NotDisposed(this);

                using (FLock.AcquireReaderLock()) 
                {
                    return FChildren.Count;
                }
            }
        }

        bool ICollection<TInterface>.IsReadOnly { get; }
      
        [CanSetParent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        void ICollection<TInterface>.Add(TInterface child)
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            Ensure.IsNull(child.Parent, $"{nameof(child)}.{nameof(child.Parent)}");
            Ensure.NotDisposed(this);

            using (FLock.AcquireWriterLock())
            {
                //
                // Ne legyen kiemelve statikusba h tesztekben megvaltoztathato legyen.
                //

                int maxChildCount = Config.Value.Composite.MaxChildCount;

                if (FChildren.Count == maxChildCount)
                    //
                    // Ha ide eljutunk az pl azt jelentheti h egy kontenerbol letrehozott injectorok 
                    // nincsenek a munkafolyamat vegen felszbaditva (a GC nem tudja felszabaditani 
                    // oket mivel a szulo kontener gyermekei).
                    //

                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.TOO_MANY_CHILDREN, maxChildCount));

                bool succeeded = FChildren.Add(child);
                Assert(succeeded, "Child already contained");
            }

            child.Parent = Self;
        }

        [CanSetParent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        bool ICollection<TInterface>.Remove(TInterface child)
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            Ensure.NotDisposed(this);

            if (child.Parent != Self) return false;

            using (FLock.AcquireWriterLock())
            {
                bool succeeded = FChildren.Remove(child);
                Assert(succeeded, "Child already removed");
            }

            child.Parent = null;

            return true;
        }

        bool ICollection<TInterface>.Contains(TInterface child) 
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            Ensure.NotDisposed(this);

            return child.Parent == Self;
        }

        [CanSetParent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        void ICollection<TInterface>.Clear()
        {
            Ensure.NotDisposed(this);

            using (FLock.AcquireWriterLock())
            {
                foreach (TInterface child in FChildren)
                {
                    child.Parent = null;
                }

                FChildren.Clear();
            }
        }

        void ICollection<TInterface>.CopyTo(TInterface[] array, int arrayIndex) => throw new NotSupportedException();

        IEnumerator<TInterface> IEnumerable<TInterface>.GetEnumerator()
        {
            Ensure.NotDisposed(this);

            return new SafeEnumerator<TInterface>(FChildren, FLock);
        }

        IEnumerator IEnumerable.GetEnumerator() => Children.GetEnumerator();
        #endregion
    }
}