using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace ExchangeSharp
{
    /// <summary>
    /// Interface for common exchange end points
    /// </summary>
    public interface IExchangeAPI : IBaseAPI,
        ICommonProvider, IAccountProvider,
        IMarketProvider, IOrderBookProvider,
        IDisposable

    {
        #region Utility Methods

        /// <summary>
        /// Normalize a symbol for use on this exchange
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
        /// Convert seconds to a period string, or throw exception if seconds invalid. Example: 60 seconds becomes 1m.
        /// </summary>
        /// <param name="seconds">Seconds</param>
        /// <returns>Period string</returns>
        string PeriodSecondsToString(int seconds);

        #endregion Utility Methods

        #region Web Socket

        /// <summary>
        /// 所有行情数据
        /// Get all tickers via web socket
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <param name="symbols">Symbols. If no symbols are specified, this will get the tickers for all symbols. NOTE: Some exchanges don't allow you to specify which symbols to return.</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetTickersWebSocket(Action<IReadOnlyCollection<KeyValuePair<string, ExchangeTicker>>> callback,
            params string[] symbols);

        /// <summary>
        /// 交易数据
        /// Get information about trades via web socket
        /// </summary>
        /// <param name="callback">Callback (symbol and trade)</param>
        /// <param name="marketSymbols">Market symbols</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetTradesWebSocket(Action<KeyValuePair<string, ExchangeTrade>> callback,
            params string[] marketSymbols);

        /// <summary>
        /// 所有变更了状态的订单详情
        /// Get the details of all changed orders via web socket
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetOrderDetailsWebSocket(Action<ExchangeOrderResult> callback);

        /// <summary>
        /// 已完成的订单
        /// Get the details of all completed orders via web socket
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetCompletedOrderDetailsWebSocket(Action<ExchangeOrderResult> callback);

        #endregion Web Socket
    }
}
