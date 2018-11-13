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
        #region Utility Methods

        /// <summary>
        /// 格式化交易对编码
        /// 把传入的编码格式化成当前交易所的编码
        /// </summary>
        /// <param name="marketSymbol">Symbol</param>
        /// <returns>Normalized symbol</returns>
        string NormalizeMarketSymbol(string marketSymbol);


        #endregion Utility Methods


        void Init(List<Currency> currencies, List<Symbol> symbols);

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
