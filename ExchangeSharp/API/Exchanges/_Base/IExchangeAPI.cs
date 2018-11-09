using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace ExchangeSharp
{
    /// <summary>
    /// Interface for common exchange end points
    /// </summary>
    public interface IExchangeAPI : IDisposable, IBaseAPI, IOrderBookProvider
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

        #region REST

        /// <summary>
        /// 获取所有币种和相关信息。包括是否启用和手续费
        /// Gets currencies and related data such as IsEnabled and TxFee (if available)
        /// </summary>
        /// <returns>Collection of Currencies</returns>
        Task<IReadOnlyDictionary<string, ExchangeCurrency>> GetCurrenciesAsync();

        /// <summary>
        /// 获取币种的存入地址和相关细节
        /// Gets the address to deposit to and applicable details.
        /// </summary>
        /// <param name="currency">Currency to get address for.</param>
        /// <param name="forceRegenerate">True to regenerate the address</param>
        /// <returns>Deposit address details (including tag if applicable, such as XRP)</returns>
        Task<ExchangeDepositDetails> GetDepositAddressAsync(string currency, bool forceRegenerate = false);

        /// <summary>
        /// 获取当前币种的存入历史
        /// Gets the deposit history for a currency
        /// </summary>
        /// <param name="currency">The currency to check. May be null.</param>
        /// <returns>Collection of ExchangeCoinTransfers</returns>
        Task<IEnumerable<ExchangeTransaction>> GetDepositHistoryAsync(string currency);

        /// <summary>
        /// 获取当前交易所所有币对
        /// Get symbols for the exchange markets
        /// </summary>
        /// <returns>Symbols</returns>
        Task<IEnumerable<string>> GetMarketSymbolsAsync();

        /// <summary>
        /// 获取当前交易所所有币对，包括可用的元数据，如最小交易规模和市场是否启用
        /// Get exchange market symbols including available metadata such as min trade size and whether the market is active
        /// </summary>
        /// <returns>Collection of ExchangeMarkets</returns>
        Task<IEnumerable<ExchangeMarket>> GetMarketSymbolsMetadataAsync();

        /// <summary>
        /// 获取最后行情数据
        /// Get latest ticker
        /// </summary>
        /// <param name="marketSymbol">Symbol</param>
        /// <returns>Latest ticker</returns>
        Task<ExchangeTicker> GetTickerAsync(string marketSymbol);

        /// <summary>
        /// 获取所有行情数据，不是所有交易所都支持
        /// Get all tickers, not all exchanges support this
        /// </summary>
        /// <returns>Key value pair of symbol and tickers array</returns>
        Task<IEnumerable<KeyValuePair<string, ExchangeTicker>>> GetTickersAsync();

        /// <summary>
        /// 获取历史交易情况
        /// Get historical trades for the exchange
        /// </summary>
        /// <param name="callback">Callback for each set of trades. Return false to stop getting trades immediately.</param>
        /// <param name="marketSymbol">Symbol to get historical data for</param>
        /// <param name="startDate">Optional start date time to start getting the historical data at, null for the most recent data. Not all exchanges support this.</param>
        /// <param name="endDate">Optional UTC end date time to start getting the historical data at, null for the most recent data. Not all exchanges support this.</param>
        Task GetHistoricalTradesAsync(Func<IEnumerable<ExchangeTrade>, bool> callback, string marketSymbol, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 最后交易情况
        /// Get the latest trades
        /// </summary>
        /// <param name="marketSymbol">Market Symbol</param>
        /// <returns>Trades</returns>
        Task<IEnumerable<ExchangeTrade>> GetRecentTradesAsync(string marketSymbol);

        /// <summary>
        /// K线数据， 开，最高，最低，收盘
        /// Get candles (open, high, low, close)
        /// </summary>
        /// <param name="marketSymbol">Market symbol to get candles for</param>
        /// <param name="periodSeconds">Period in seconds to get candles for. Use 60 for minute, 3600 for hour, 3600*24 for day, 3600*24*30 for month.</param>
        /// <param name="startDate">Optional start date to get candles for</param>
        /// <param name="endDate">Optional end date to get candles for</param>
        /// <param name="limit">Max results, can be used instead of startDate and endDate if desired</param>
        /// <returns>Candles</returns>
        Task<IEnumerable<MarketCandle>> GetCandlesAsync(string marketSymbol, int periodSeconds, DateTime? startDate = null, DateTime? endDate = null, int? limit = null);

        /// <summary>
        /// 获取货币数量
        /// Get total amounts, symbol / amount dictionary
        /// </summary>
        /// <returns>Dictionary of symbols and amounts</returns>
        Task<Dictionary<string, decimal>> GetAmountsAsync();

        /// <summary>
        /// 可交易的货币数量
        /// Get amounts available to trade, symbol / amount dictionary
        /// </summary>
        /// <returns>Dictionary of symbols and amounts available to trade</returns>
        Task<Dictionary<string, decimal>> GetAmountsAvailableToTradeAsync();

        /// <summary>
        /// 提交订单
        /// Place an order
        /// </summary>
        /// <param name="order">Order request</param>
        /// <returns>Order result and message string if any</returns>
        Task<ExchangeOrderResult> PlaceOrderAsync(ExchangeOrderRequest order);

        /// <summary>
        /// Place bulk orders
        /// </summary>
        /// <param name="orders">Order requests</param>
        /// <returns>Order results, each result matches up with each order in index</returns>
        Task<ExchangeOrderResult[]> PlaceOrdersAsync(params ExchangeOrderRequest[] orders);

        /// <summary>
        /// 获取订单详情
        /// Get details of an order
        /// </summary>
        /// <param name="orderId">order id</param>
        /// <param name="marketSymbol">Market Symbol</param>
        /// <returns>Order details</returns>
        Task<ExchangeOrderResult> GetOrderDetailsAsync(string orderId, string marketSymbol = null);

        /// <summary>
        /// 获取所有未完成的订单详情
        /// Get the details of all open orders
        /// </summary>
        /// <param name="marketSymbol">Market symbol to get open orders for or null for all</param>
        /// <returns>All open order details for the specified symbol</returns>
        Task<IEnumerable<ExchangeOrderResult>> GetOpenOrderDetailsAsync(string marketSymbol = null);

        /// <summary>
        /// 获取所有完成的订单详情
        /// Get the details of all completed orders
        /// </summary>
        /// <param name="marketSymbol">Market symbol to get completed orders for or null for all</param>
        /// <param name="afterDate">Only returns orders on or after the specified date/time</param>
        /// <returns>All completed order details for the specified symbol, or all if null symbol</returns>
        Task<IEnumerable<ExchangeOrderResult>> GetCompletedOrderDetailsAsync(string marketSymbol = null, DateTime? afterDate = null);

        /// <summary>
        /// 取消订单
        /// Cancel an order, an exception is thrown if failure
        /// </summary>
        /// <param name="orderId">Order id of the order to cancel</param>
        /// <param name="marketSymbol">Market symbol of the order to cancel (not required for most exchanges)</param>
        Task CancelOrderAsync(string orderId, string marketSymbol = null);

        /// <summary>
        /// 获取可用于交易的保证金金额
        /// Get margin amounts available to trade, symbol / amount dictionary
        /// </summary>
        /// <param name="includeZeroBalances">Include currencies with zero balance in return value</param>
        /// <returns>Dictionary of symbols and amounts available to trade in margin account</returns>
        Task<Dictionary<string, decimal>> GetMarginAmountsAvailableToTradeAsync(bool includeZeroBalances = false);

        /// <summary>
        /// 获取未平仓的款项
        /// Get open margin position
        /// </summary>
        /// <param name="marketSymbol">Market Symbol</param>
        /// <returns>Open margin position result</returns>
        Task<ExchangeMarginPositionResult> GetOpenPositionAsync(string marketSymbol);

        /// <summary>
        /// 平仓
        /// Close a margin position
        /// </summary>
        /// <param name="marketSymbol">Market Symbol</param>
        /// <returns>Close margin position result</returns>
        Task<ExchangeCloseMarginPositionResult> CloseMarginPositionAsync(string marketSymbol);
         
        /// <summary>
        /// 手续费
        /// Get fees
        /// </summary>
        /// <returns>The customer trading fees</returns>
        Task<Dictionary<string, decimal>> GetFeesAync();

        #endregion REST

        #region Web Socket

        /// <summary>
        /// 所有行情数据
        /// Get all tickers via web socket
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <param name="symbols">Symbols. If no symbols are specified, this will get the tickers for all symbols. NOTE: Some exchanges don't allow you to specify which symbols to return.</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetTickersWebSocket(Action<IReadOnlyCollection<KeyValuePair<string, ExchangeTicker>>> callback, params string[] symbols);

        /// <summary>
        /// 交易数据
        /// Get information about trades via web socket
        /// </summary>
        /// <param name="callback">Callback (symbol and trade)</param>
        /// <param name="marketSymbols">Market symbols</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetTradesWebSocket(Action<KeyValuePair<string, ExchangeTrade>> callback, params string[] marketSymbols);

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
