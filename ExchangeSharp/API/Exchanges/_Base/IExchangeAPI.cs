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

        //todo：这块编码转换，晚点在看怎么处理

        #region Utility Methods

        /// <summary>
        /// 格式化交易对编码
        /// 把传入的编码格式化成当前交易所的编码
        /// </summary>
        /// <param name="marketSymbol">Symbol</param>
        /// <returns>Normalized symbol</returns>
        string NormalizeMarketSymbol(string marketSymbol);

        /// <summary>
        /// Convert an exchange symbol into a global symbol, which will be the same for all exchanges.
        /// Global symbols are always uppercase and separate the currency pair with a hyphen (-).
        /// Global symbols list the base currency first (i.e. BTC) and conversion currency
        /// second (i.e. USD). Example BTC-USD, read as x BTC is worth y USD.
        /// </summary>
        /// <param name="marketSymbol">Exchange symbol</param>
        /// <returns>Global symbol</returns>
        string ExchangeMarketSymbolToGlobalMarketSymbol(string marketSymbol);

        /// <summary>
        /// Convert a global symbol into an exchange symbol, which will potentially be different from other exchanges.
        /// </summary>
        /// <param name="marketSymbol">Global symbol</param>
        /// <returns>Exchange symbol</returns>
        string GlobalMarketSymbolToExchangeMarketSymbol(string marketSymbol);

        /// <summary>
        /// 把秒转换为周期字符串 ， 例如60秒转换后为1m
        /// 主要是给k线用的
        /// Convert seconds to a period string, or throw exception if seconds invalid. Example: 60 seconds becomes 1m.
        /// </summary>
        /// <param name="seconds">Seconds</param>
        /// <returns>Period string</returns>
        string PeriodSecondsToString(int seconds);

        #endregion Utility Methods


        /// <summary>
        /// 可用币种
        /// </summary>
        List<ExchangeCurrency> Currencies { get; set; }

        /// <summary>
        /// 币对信息
        /// </summary>
        List<ExchangeMarket> Symbols { get; set; }


    }
}
