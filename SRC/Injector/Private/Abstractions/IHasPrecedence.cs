/********************************************************************************
* IHasPrecedence.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal interface IHasPrecedence
    {
        public int Precedence { get; }
    }
}
