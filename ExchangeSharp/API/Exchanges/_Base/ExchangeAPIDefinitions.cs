/*

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeSharp
{
    /// <summary>
    /// This shows all the methods that can be overriden when implementation a new exchange, along
    /// with all the fields that should be set in the constructor or static constructor if needed.
    /// </summary>
    public abstract partial class ExchangeAPI
    {
        /*
        protected virtual Task<IEnumerable<KeyValuePair<string, ExchangeTicker>>> OnGetTickersAsync();
        protected virtual Task<IEnumerable<KeyValuePair<string, ExchangeOrderBook>>> OnGetOrderBooksAsync(int maxCount = 100);
        protected virtual Task<IEnumerable<ExchangeTrade>> OnGetRecentTradesAsync(string symbol);
        protected virtual Task<IReadOnlyDictionary<string, ExchangeCurrency>> OnGetCurrenciesAsync();
        protected virtual Task<IEnumerable<string>> OnGetSymbolsAsync();
        protected virtual Task<IEnumerable<ExchangeMarket>> OnGetSymbolsMetadataAsync();
        protected virtual Task<ExchangeTicker> OnGetTickerAsync(string symbol);
        protected virtual Task<ExchangeOrderBook> OnGetOrderBookAsync(string symbol, int maxCount = 100);
        protected virtual OnGetHistoricalTradesAsync(Func<IEnumerable<ExchangeTrade>, bool> callback, string symbol, DateTime? startDate = null, DateTime? endDate = null);
        protected virtual Task<ExchangeDepositDetails> OnGetDepositAddressAsync(string symbol, bool forceRegenerate = false);
        protected virtual Task<IEnumerable<ExchangeTransaction>> OnGetDepositHistoryAsync(string symbol);
        protected virtual Task<IEnumerable<MarketCandle>> OnGetCandlesAsync(string symbol, int periodSeconds, DateTime? startDate = null, DateTime? endDate = null, int? limit = null);
        protected virtual Task<Dictionary<string, decimal>> OnGetAmountsAsync();
        protected virtual Task<Dictionary<string, decimal>> OnGetFeesAsync();
        protected virtual Task<Dictionary<string, decimal>> OnGetAmountsAvailableToTradeAsync();
        protected virtual Task<ExchangeOrderResult> OnPlaceOrderAsync(ExchangeOrderRequest order);
        protected virtual Task<ExchangeOrderResult[]> OnPlaceOrdersAsync(params ExchangeOrderRequest[] order);
        protected virtual Task<ExchangeOrderResult> OnGetOrderDetailsAsync(string orderId, string symbol = null);
        protected virtual Task<IEnumerable<ExchangeOrderResult>> OnGetOpenOrderDetailsAsync(string symbol = null);
        protected virtual Task<IEnumerable<ExchangeOrderResult>> OnGetCompletedOrderDetailsAsync(string symbol = null, DateTime? afterDate = null);
        protected virtual Task OnCancelOrderAsync(string orderId, string symbol = null);
        protected virtual Task<ExchangeWithdrawalResponse> OnWithdrawAsync(ExchangeWithdrawalRequest withdrawalRequest);
        protected virtual Task<Dictionary<string, decimal>> OnGetMarginAmountsAvailableToTradeAsync();
        protected virtual Task<ExchangeMarginPositionResult> OnGetOpenPositionAsync(string symbol);
        protected virtual Task<ExchangeCloseMarginPositionResult> OnCloseMarginPositionAsync(string symbol);

        protected virtual IWebSocket OnGetTickersWebSocket(Action<IReadOnlyCollection<KeyValuePair<string, ExchangeTicker>>> tickers);
        protected virtual IWebSocket OnGetTradesWebSocket(Action<KeyValuePair<string, ExchangeTrade>> callback, params string[] symbols);
        protected virtual IWebSocket OnGetOrderBookWebSocket(Action<ExchangeOrderBook> callback, int maxCount = 20, params string[] symbols);
        protected virtual IWebSocket OnGetOrderDetailsWebSocket(Action<ExchangeOrderResult> callback);
        protected virtual IWebSocket OnGetCompletedOrderDetailsWebSocket(Action<ExchangeOrderResult> callback);

        // these generally do not need to be overriden unless your Exchange does something funny or does not use a symbol separator
        public virtual string NormalizeSymbol(string symbol);
        public virtual string ExchangeSymbolToGlobalSymbol(string symbol);
        public virtual string GlobalSymbolToExchangeSymbol(string symbol);
        public virtual string PeriodSecondsToString(int seconds);

        protected virtual void OnDispose();
        */

        /// <summary>
        /// Dictionary of key (exchange currency) and value (global currency). Add entries in static constructor.
        /// Some exchanges (Yobit for example) use odd names for some currencies like BCC for Bitcoin Cash.
        /// <example><![CDATA[ 
        /// ExchangeGlobalCurrencyReplacements[typeof(ExchangeYobitAPI)] = new KeyValuePair<string, string>[]
        /// {
        ///     new KeyValuePair<string, string>("BCC", "BCH")
        /// };
        /// ]]></example>
        /// </summary>
        protected static readonly Dictionary<Type, KeyValuePair<string, string>[]> ExchangeGlobalCurrencyReplacements = new Dictionary<Type, KeyValuePair<string, string>[]>();

        /// <summary>
        /// 币对的分隔符。如果不是连字符，则在构造函数中设置。
        /// Separator for exchange symbol. If not a hyphen, set in constructor.
        /// </summary>
        public string MarketSymbolSeparator { get; protected set; } = "-";

        /// <summary>
        /// 符号是否颠倒。大多数exchange都执行ETH-BTC，如果您的exchange执行bc - eth，则在构造函数中将其设置为true。
        /// Whether the symbol is reversed. Most exchanges do ETH-BTC, if your exchange does BTC-ETH, set to true in constructor.
        /// </summary>
        public bool MarketSymbolIsReversed { get; protected set; }

        /// <summary>
        /// 币对符号是否是大写
        /// Whether the symbol is uppercase. Most exchanges are true, but if your exchange is lowercase, set to false in constructor.
        /// </summary>
        public bool MarketSymbolIsUppercase { get; protected set; } = true;

        /// <summary>
        /// Websocket深度信息的类型
        /// 例如：一开始是全集，后面增量、全部是增量、全部是全集 等
        /// The type of web socket order book supported
        /// </summary>
        public WebSocketOrderBookType WebSocketOrderBookType { get; protected set; } = WebSocketOrderBookType.None;
    }

    // implement this and change the field name and value to the name of your exchange
    // public partial class ExchangeName { public const string MyNewExchangeName = "MyNewExchangeName"; }
}
