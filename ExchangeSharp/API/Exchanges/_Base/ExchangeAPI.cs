using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// Base class for all exchange API
    /// </summary>
    public abstract partial class ExchangeAPI : BaseAPI, IExchangeAPI
    {
        #region Private methods

        private static readonly Dictionary<string, IExchangeAPI> Apis = new Dictionary<string, IExchangeAPI>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        /// <summary>
        /// 时间戳类型
        /// </summary>
        protected TimestampType TimestampType { get; set; } = TimestampType.UnixMilliseconds;

  
        #region API Implementation

        protected virtual Task<Dictionary<string, decimal>> OnGetAmountsAsync() => throw new NotImplementedException();
        #endregion API implementation


        /// <summary>
        /// 利用交易对的相关信息，控制价格。使他符合标准
        /// Clamp price using market info. If necessary, a network request will be made to retrieve symbol metadata.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="outputPrice">Price</param>
        /// <returns>Clamped price</returns>
        protected decimal ClampOrderPrice(Symbol symbol, decimal outputPrice)
        {
            return symbol == null ? outputPrice : CryptoUtility.ClampDecimal(symbol.MinPrice, symbol.MaxPrice, symbol.PriceStepSize, outputPrice);
        }

        /// <summary>
        /// 利用交易对的信息，控制数量。使他符合标准
        /// Clamp quantiy using market info. If necessary, a network request will be made to retrieve symbol metadata.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="outputQuantity">Quantity</param>
        /// <returns>Clamped quantity</returns>
        protected decimal ClampOrderQuantity(Symbol symbol, decimal outputQuantity)
        {
            return symbol == null ? outputQuantity : CryptoUtility.ClampDecimal(symbol.MinTradeSize, symbol.MaxTradeSize, symbol.QuantityStepSize, outputQuantity);
        }

      
        /// <summary>
        /// Override to dispose of resources when the exchange is disposed
        /// </summary>
        protected virtual void OnDispose() { }

        #endregion Protected methods

        #region Other

        /// <summary>
        /// Static constructor
        /// </summary>
        static ExchangeAPI()
        {
            foreach (Type type in typeof(ExchangeAPI).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(ExchangeAPI)) && !type.IsAbstract))
            {
                // lazy create, we just create an instance to get the name, nothing more
                // we don't want to pro-actively create all of these becanse an API
                // may be running a timer or other house-keeping which we don't want
                // the overhead of if a user is only using one or a handful of the apis
                using (ExchangeAPI api = Activator.CreateInstance(type) as ExchangeAPI)
                {

                    if(string.IsNullOrEmpty(api.Name))
                        continue;

                    Apis[api.Name] = null;
                }

                // in case derived class is accessed first, check for existance of key
                if (!ExchangeGlobalCurrencyReplacements.ContainsKey(type))
                {
                    ExchangeGlobalCurrencyReplacements[type] = new KeyValuePair<string, string>[0];
                }
            }
        }


        /// <summary>
        /// Finalizer
        /// </summary>
        ~ExchangeAPI()
        {
            Dispose();
        }

        /// <summary>
        /// Dispose and cleanup all resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                OnDispose();

                // take out of global api dictionary if disposed
                lock (Apis)
                {
                    if (Apis.TryGetValue(Name, out var cachedApi) && cachedApi == this)
                    {
                        Apis[Name] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Get an exchange API given an exchange name (see ExchangeName class)
        /// </summary>
        /// <param name="exchangeName">Exchange name</param>
        /// <returns>Exchange API or null if not found</returns>
        public static IExchangeAPI GetExchangeAPI(string exchangeName)
        {
            // note: this method will be slightly slow (milliseconds) the first time it is called and misses the cache
            // subsequent calls with cache hits will be nanoseconds
            lock (Apis)
            {
                if (!Apis.TryGetValue(exchangeName, out IExchangeAPI api))
                {
                    throw new ArgumentException("No API available with name " + exchangeName);
                }
                if (api == null)
                {
                    // find an API with the right name
                    foreach (Type type in typeof(ExchangeAPI).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(ExchangeAPI)) && !type.IsAbstract))
                    {
                        api = Activator.CreateInstance(type) as IExchangeAPI;
                        if (api.Name == exchangeName)
                        {
                            // found one with right name, add it to the API dictionary
                            Apis[exchangeName] = api;

                            // break out, we are done
                            break;
                        }
                        else
                        {
                            // name didn't match, dispose immediately to stop timers and other nasties we don't want running, and null out api variable
                            api.Dispose();
                            api = null;
                        }
                    }
                }
                return api;
            }
        }

        /// <summary>
        /// Get all exchange APIs
        /// </summary>
        /// <returns>All APIs</returns>
        public static IExchangeAPI[] GetExchangeAPIs()
        {
            lock (Apis)
            {
                List<IExchangeAPI> apiList = new List<IExchangeAPI>();
                foreach (var kv in Apis.ToArray())
                {
                    if (kv.Value == null)
                    {
                        apiList.Add(GetExchangeAPI(kv.Key));
                    }
                    else
                    {
                        apiList.Add(kv.Value);
                    }
                }
                return apiList.ToArray();
            }
        }

        /// <summary>
        /// 初始化，设置币种和币对信息
        /// </summary>
        /// <param name="currencies"></param>
        /// <param name="symbols"></param>
        public void LoadInformation(List<Currency> currencies, List<Symbol> symbols)
        {
            this.Currencies = currencies;
            this.Symbols = symbols;
        }

        public List<Currency> Currencies { get; private set; } = new List<Currency>();
        public List<Symbol> Symbols { get; private set; } = new List<Symbol>();

        #endregion Other
   
        #region  common

            /// <summary>
        /// Gets currencies and related data such as IsEnabled and TxFee (if available)
        /// </summary>
        /// <returns>Collection of Currencies</returns>
        public abstract Task<List<Currency>> GetCurrenciesAsync();

        /// <summary>
        /// Get exchange symbols including available metadata such as min trade size and whether the market is active
        /// </summary>
        /// <returns>Collection of ExchangeMarkets</returns>
        public abstract Task<List<Symbol>> GetSymbolsAsync();

        #endregion

        #region  ticker

           /// <summary>
        /// 获取单个币对的ticker
        /// </summary>
        /// <returns>Ticker</returns>
        public abstract Task<ExchangeTicker> GetTickerAsync(Symbol symbol);


        /// <summary>
        /// Get all tickers in one request. If the exchange does not support this, a ticker will be requested for each symbol.
        /// </summary>
        /// <returns>Key value pair of symbol and tickers array</returns>
        public abstract Task<List<ExchangeTicker>> GetTickersAsync();

        #endregion

        #region  trade

        /// <summary>
        /// Get recent trades on the exchange - the default implementation simply calls GetHistoricalTrades with a null sinceDateTime.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="limit"></param>
        /// <returns>An enumerator that loops through all recent trades</returns>
        public abstract  Task<List<ExchangeTrade>> GetTradesAsync(Symbol symbol, int limit);

        #endregion

        #region depth

        /// <summary>
        /// Get exchange Depth
        /// </summary>
        /// <returns>Exchange order book or null if failure</returns>
        public abstract Task<ExchangeDepth> GetDepthAsync(Symbol symbol, int maxCount);


        #endregion

        #region candles

          /// <summary>
        /// Get candles (open, high, low, close)
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="periodSeconds">Period in seconds to get candles for. Use 60 for minute, 3600 for hour, 3600*24 for day, 3600*24*30 for month.</param>
        /// <param name="startDate">Optional start date to get candles for</param>
        /// <param name="endDate">Optional end date to get candles for</param>
        /// <param name="limit">Max results, can be used instead of startDate and endDate if desired</param>
        /// <returns>Candles</returns>
        public abstract Task<List<MarketCandle>> GetCandlesAsync(Symbol symbol, int periodSeconds,
            DateTime? startDate = null, DateTime? endDate = null, int? limit = null);


        public abstract IWebSocket GetCandlesWebSocket(Action<MarketCandle> callback, int periodSeconds, Symbol[] symbols);

        #endregion

        #region order

        /// <summary>
        /// Place an order
        /// </summary>
        /// <param name="orders"></param>
        /// <returns>Result</returns>
        public abstract Task<List<ExchangeOrderResult>> PlaceOrdersAsync(params ExchangeOrderRequest[] orders);

        #endregion

        /// <summary>
        /// Cancel an order, an exception is thrown if error
        /// </summary>
        /// <param name="orders"></param>
        public abstract Task CancelOrdersAsync(params ExchangeOrderCancelRequest[] orders);


 
        /// <summary>
        /// Get total amounts, symbol / amount dictionary
        /// </summary>
        /// <returns>Dictionary of symbols and amounts</returns>
        public async Task<Dictionary<string, decimal>> GetAmountsAsync()
        {
            return await OnGetAmountsAsync();
        }


        /// <summary>
        /// Get the details of all open orders
        /// </summary>
        /// <returns>All open order details</returns>
        public abstract Task<IEnumerable<ExchangeOrderResult>> GetOpenOrderDetailsAsync(Symbol symbol = null);

        /// <summary>
        /// Get the details of all completed orders
        /// </summary>
        /// <param name="symbol">Symbol to get completed orders for or null for all</param>
        /// <param name="afterDate">Only returns orders on or after the specified date/time</param>
        /// <returns>All completed order details for the specified symbol, or all if null symbol</returns>
        public abstract  Task<IEnumerable<ExchangeOrderResult>> GetCompletedOrderDetailsAsync(Symbol symbol = null,
            DateTime? afterDate = null);


        #region Web Socket API

        /// <summary>
        /// Get all tickers via web socket
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <param name="symbols"></param>
        /// <returns>Web socket, call Dispose to close</returns>
        public abstract IWebSocket GetTickerWebSocket(Action<ExchangeTicker> callback, params Symbol[] symbols);


        /// <summary>
        /// Get information about trades via web socket
        /// </summary>
        /// <param name="callback">Callback (symbol and trade)</param>
        /// <param name="symbols">Market Symbols</param>
        /// <returns>Web socket, call Dispose to close</returns>
        public abstract IWebSocket GetTradesWebSocket(Action<List<ExchangeTrade>> callback, params Symbol[] symbols);

        /// <summary>
        /// Get delta order book bids and asks via web socket. Only the deltas are returned for each callback. To manage a full order book, use ExchangeAPIExtensions.GetDepthWebSocket.
        /// </summary>
        /// <param name="callback">Callback of symbol, order book</param>
        /// <param name="maxCount">Max count of bids and asks - not all exchanges will honor this parameter</param>
        /// <param name="marketSymbols">Market symbols or null/empty for all of them (if supported)</param>
        /// <returns>Web socket, call Dispose to close</returns>
        public abstract IWebSocket GetDepthWebSocket(Action<ExchangeDepth> callback, int maxCount = 20,
            params Symbol[] marketSymbols);


        #endregion Web Socket API
    }
}
