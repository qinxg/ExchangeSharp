using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// Interface for common exchange end points
    /// </summary>
    public interface IExchangeAPI : IBaseAPI,
        ICommonProvider, IAccountProvider,
        IMarketProvider, IDepthProvider,
        IOrderProvider,
        IDisposable

    {

        void LoadInformation(List<Currency> currencies, List<Symbol> symbols);

        /// <summary>
        /// 可用币种
        /// </summary>
        List<Currency> Currencies { get; }

        /// <summary>
        /// 币对信息
        /// </summary>
        List<Symbol> Symbols { get;  }
    }
}
