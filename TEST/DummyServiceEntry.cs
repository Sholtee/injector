/********************************************************************************
* DummyServiceEntry.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI
{
    using Internals;

    public class DummyServiceEntry : AbstractServiceEntry
    {
        private interface IDummyService { }

        public DummyServiceEntry() : base(typeof(IDummyService), null)
        {
        }
    }
}
