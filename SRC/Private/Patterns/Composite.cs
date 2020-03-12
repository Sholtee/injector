/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
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
    public abstract class Composite<TInterface> : Disposable, IComposite<TInterface> where TInterface : class, IComposite<TInterface>
    {
        private readonly HashSet<TInterface> FChildren = new HashSet<TInterface>();

        //
        // Az [Add|Remove]Child() lehet hivva parhuzamosan ezert szalbiztosnak kell legyunk.
        //

        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim();

        private readonly WriteOnce<TInterface> FParent = new WriteOnce<TInterface>(strict: false);

        private bool FDisposing;

        private TInterface Self { get; }

        #region Protected
        /// <summary>
        /// Creates a new <see cref="Composite{TInterface}"/> instance.
        /// </summary>
        /// <param name="parent">The parent entity. It can be null.</param>
        protected Composite(TInterface parent)
        {
            Self = this as TInterface;
            Assert(Self != null);

            if (parent == null)
                //
                // Ha nincs szulo akkor kesobbiekben mar nem is lehet beallitani.
                //

                Parent = parent;
            else
                parent.AddChild(Self);
        }

        /// <summary>
        /// Disposal logic related to this class.
        /// </summary>
        /// <param name="disposeManaged">Check out the <see cref="Disposable"/> class.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                FDisposing = true;

                //
                // Kivesszuk magunkat a szulo gyerekei kozul (kiveve ha gyoker elemunk van, ott nincs szulo).
                //

                Parent?.RemoveChild(Self);

                //
                // Osszes gyereket Dispose()-oljuk. Mivel a Children amugy is masolatot ad vissza ezert
                // iteracio kozben is kivehetunk elemet a listabol.
                //

                foreach (IDisposable child in Children)
                {
                    child.Dispose();
                }

                Assert(!FChildren.Any());

                FLock.Dispose();
            }

            base.Dispose(disposeManaged);
        }
#if !NETSTANDARD1_6
        /// <summary>
        /// Disposal logic related to this class.
        /// </summary>
        protected override async ValueTask AsyncDispose()
        {
            FDisposing = true;

            Parent?.RemoveChild(Self);

            foreach (IAsyncDisposable child in Children)
                await child.DisposeAsync();

            Assert(!FChildren.Any());

            FLock.Dispose();
        }
#endif
        #endregion

        #region IComposite
        /// <summary>
        /// The parent of this entity. Can be null.
        /// </summary>
        public TInterface Parent
        {
            get => FParent;
            set
            {
                if (!FDisposing)
                    FParent.Value = value;
            }
        }

        /// <summary>
        /// The children of this entity.
        /// </summary>
        public virtual IReadOnlyCollection<TInterface> Children
        {
            get
            {
                Ensure.NotDisposed(this);

                using (FLock.AcquireReaderLock())
                {
                    //
                    // Masolatot adjunk vissza.
                    //

                    return FChildren.ToArray();
                }
            }
        }

        /// <summary>
        /// Adds a new child to the <see cref="Children"/> list. For more information see the <see cref="IComposite{T}"/> interface.
        /// </summary>
        public virtual void AddChild(TInterface child)
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
                Assert(succeeded, $"Child (${child}) already contained");
            }

            child.Parent = Self;
        }

        /// <summary>
        /// Removes a child from the <see cref="Children"/> list. For more information see the <see cref="IComposite{T}"/> interface.
        /// </summary>
        public virtual void RemoveChild(TInterface child)
        {
            Ensure.Parameter.IsNotNull(child, nameof(child));
            Ensure.AreEqual(child.Parent, Self, Resources.INAPPROPRIATE_OWNERSHIP);
            Ensure.NotDisposed(this);

            //
            // Composite leszarmazott gyereket csak Dispose() hivassal lehet eltavolitani.
            //

            try
            {
                child.Parent = null;
            }
            catch (InvalidOperationException e) when (e.Message == Resources.VALUE_ALREADY_SET) 
            {
                throw new InvalidOperationException(Resources.CANT_REMOVE_CHILD, e);
            }

            using (FLock.AcquireWriterLock())
            {
                bool succeeded = FChildren.Remove(child);
                Assert(succeeded, $"Child (${child}) already removed");
            }
        }
        #endregion
    }
}