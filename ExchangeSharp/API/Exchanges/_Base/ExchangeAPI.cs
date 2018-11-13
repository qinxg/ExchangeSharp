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


        #endregion Private methods

        #region API Implementation


        protected virtual async Task<IEnumerable<KeyValuePair<string, ExchangeOrderBook>>> OnGetOrderBooksAsync(int maxCount = 100)
        {
            //List<KeyValuePair<string, ExchangeOrderBook>> books = new List<KeyValuePair<string, ExchangeOrderBook>>();
            //var marketSymbols = await GetMarketSymbolsAsync();
            //foreach (string marketSymbol in marketSymbols)
            //{
            //    var book = await GetDepthAsync(marketSymbol);
            //    books.Add(new KeyValuePair<string, ExchangeOrderBook>(marketSymbol, book));
            //}
            //return books;

            return null;
        }



        protected virtual Task<ExchangeOrderBook> OnGetOrderBookAsync(string marketSymbol, int maxCount = 100) => throw new NotImplementedException();
        protected virtual Task<ExchangeDepositDetails> OnGetDepositAddressAsync(string currency, bool forceRegenerate = false) => throw new NotImplementedException();
        protected virtual Task<IEnumerable<ExchangeTransaction>> OnGetDepositHistoryAsync(string currency) => throw new NotImplementedException();
        protected virtual Task<Dictionary<string, decimal>> OnGetAmountsAsync() => throw new NotImplementedException();
        protected virtual Task<Dictionary<string, decimal>> OnGetFeesAsync() => throw new NotImplementedException();
        protected virtual Task<Dictionary<string, decimal>> OnGetAmountsAvailableToTradeAsync() => throw new NotImplementedException();
        protected virtual Task<ExchangeOrderResult> OnPlaceOrderAsync(ExchangeOrderRequest order) => throw new NotImplementedException();
        protected virtual Task<ExchangeOrderResult[]> OnPlaceOrdersAsync(params ExchangeOrderRequest[] order) => throw new NotImplementedException();
        protected virtual Task<ExchangeOrderResult> OnGetOrderDetailsAsync(string orderId, string marketSymbol = null) => throw new NotImplementedException();
        protected virtual Task<IEnumerable<ExchangeOrderResult>> OnGetOpenOrderDetailsAsync(string marketSymbol = null) => throw new NotImplementedException();
        protected virtual Task<IEnumerable<ExchangeOrderResult>> OnGetCompletedOrderDetailsAsync(string marketSymbol = null, DateTime? afterDate = null) => throw new NotImplementedException();
        protected virtual Task OnCancelOrderAsync(string orderId, string marketSymbol = null) => throw new NotImplementedException();
        protected virtual Task<ExchangeWithdrawalResponse> OnWithdrawAsync(ExchangeWithdrawalRequest withdrawalRequest) => throw new NotImplementedException();
        protected virtual Task<Dictionary<string, decimal>> OnGetMarginAmountsAvailableToTradeAsync(bool includeZeroBalances) => throw new NotImplementedException();
        protected virtual Task<ExchangeMarginPositionResult> OnGetOpenPositionAsync(string marketSymbol) => throw new NotImplementedException();
        protected virtual Task<ExchangeCloseMarginPositionResult> OnCloseMarginPositionAsync(string marketSymbol) => throw new NotImplementedException();

        protected virtual IWebSocket OnGetTickersWebSocket(Action<IReadOnlyCollection<KeyValuePair<string, ExchangeTicker>>> tickers, params string[] marketSymbols) => throw new NotImplementedException();
        protected virtual IWebSocket OnGetTradesWebSocket(Action<KeyValuePair<string, ExchangeTrade>> callback, params string[] marketSymbols) => throw new NotImplementedException();
        protected virtual IWebSocket OnGetOrderBookWebSocket(Action<ExchangeOrderBook> callback, int maxCount = 20, params string[] marketSymbols) => throw new NotImplementedException();
        protected virtual IWebSocket OnGetOrderDetailsWebSocket(Action<ExchangeOrderResult> callback) => throw new NotImplementedException();
        protected virtual IWebSocket OnGetCompletedOrderDetailsWebSocket(Action<ExchangeOrderResult> callback) => throw new NotImplementedException();

        #endregion API implementation

        #region Protected methods

        /// <summary>
        /// 利用交易对的相关信息，控制价格。使他符合标准
        /// Clamp price using market info. If necessary, a network request will be made to retrieve symbol metadata.
        /// </summary>
        /// <param name="marketSymbol">Market Symbol</param>
        /// <param name="outputPrice">Price</param>
        /// <returns>Clamped price</returns>
        protected decimal ClampOrderPrice(string marketSymbol, decimal outputPrice)
        {
            Symbol market =  GetExchangeMarketFromCacheAsync(marketSymbol);
            return market == null ? outputPrice : CryptoUtility.ClampDecimal(market.MinPrice, market.MaxPrice, market.PriceStepSize, outputPrice);
        }

        /// <summary>
        /// 利用交易对的信息，控制数量。使他符合标准
        /// Clamp quantiy using market info. If necessary, a network request will be made to retrieve symbol metadata.
        /// </summary>
        /// <param name="marketSymbol">Market Symbol</param>
        /// <param name="outputQuantity">Quantity</param>
        /// <returns>Clamped quantity</returns>
        protected decimal ClampOrderQuantity(string marketSymbol, decimal outputQuantity)
        {
            Symbol market =  GetExchangeMarketFromCacheAsync(marketSymbol);
            return market == null ? outputQuantity : CryptoUtility.ClampDecimal(market.MinTradeSize, market.MaxTradeSize, market.QuantityStepSize, outputQuantity);
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
        /// Normalize an exchange specific symbol. The symbol should already be in the correct order,
        /// this method just deals with casing and putting in the right separator.
        /// </summary>
        /// <param name="marketSymbol">Symbol</param>
        /// <returns>Normalized symbol</returns>
        public virtual string NormalizeMarketSymbol(string marketSymbol)
        {
            marketSymbol = (marketSymbol ?? string.Empty).Trim();
            marketSymbol = marketSymbol.Replace("-", MarketSymbolSeparator)
                .Replace("/", MarketSymbolSeparator)
                .Replace("_", MarketSymbolSeparator)
                .Replace(" ", MarketSymbolSeparator)
                .Replace(":", MarketSymbolSeparator);
            if (MarketSymbolIsUppercase)
            {
                return marketSymbol.ToUpperInvariant();
            }
            return marketSymbol.ToLowerInvariant();
        }


        /// <summary>
        /// 初始化，设置币种和币对信息
        /// </summary>
        /// <param name="currencies"></param>
        /// <param name="symbols"></param>
        public void Init(List<Currency> currencies, List<Symbol> symbols)
        {
            this.Currencies = currencies;
            this.Symbols = symbols;
        }

        public List<Currency> Currencies { get; private set; } = new List<Currency>();
        public List<Symbol> Symbols { get; private set; } = new List<Symbol>();

        #endregion Other

        #region REST API

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

        #region  trades

        /// <summary>
        /// Get recent trades on the exchange - the default implementation simply calls GetHistoricalTrades with a null sinceDateTime.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="limit"></param>
        /// <returns>An enumerator that loops through all recent trades</returns>
        public abstract  Task<List<ExchangeTrade>> GetTradesAsync(Symbol symbol, int limit);

        #endregion

        /// <summary>
        /// Gets the exchange market from this exchange's SymbolsMetadata cache. This will make a network request if needed to retrieve fresh markets from the exchange using GetSymbolsMetadataAsync().
        /// Please note that sending a symbol that is not found over and over will result in many network requests. Only send symbols that you are confident exist on the exchange.
        /// </summary>
        /// <param name="marketSymbol">The market symbol. Ex. ADA/BTC. This is assumed to be normalized and already correct for the exchange.</param>
        /// <returns>The Symbol or null if it doesn't exist in the cache or there was an error</returns>
        public Symbol GetExchangeMarketFromCacheAsync(string marketSymbol)
        {
            return Symbols.FirstOrDefault(m => m.OriginSymbol == marketSymbol);
        }

        /// <summary>
        /// Get exchange order book
        /// </summary>
        /// <param name="marketSymbol">Symbol to get order book for</param>
        /// <param name="maxCount">Max count, not all exchanges will honor this parameter</param>
        /// <returns>Exchange order book or null if failure</returns>
        public  async Task<ExchangeOrderBook> GetDepthAsync(string marketSymbol, int maxCount = 100)
        {
            marketSymbol = NormalizeMarketSymbol(marketSymbol);
            return await OnGetOrderBookAsync(marketSymbol, maxCount);
        }


        /// <summary>
        /// Get all exchange order book symbols in one request. If the exchange does not support this, an order book will be requested for each symbol. Depending on the exchange, the number of bids and asks will have different counts, typically 50-100.
        /// </summary>
        /// <param name="maxCount">Max count of bids and asks - not all exchanges will honor this parameter</param>
        /// <returns>Symbol and order books pairs</returns>
        public  async Task<IEnumerable<KeyValuePair<string, ExchangeOrderBook>>> GetAllDepthAsync(int maxCount = 100)
        {
            return await OnGetOrderBooksAsync(maxCount);
        }


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


        /// <summary>
        /// Get total amounts, symbol / amount dictionary
        /// </summary>
        /// <returns>Dictionary of symbols and amounts</returns>
        public  async Task<Dictionary<string, decimal>> GetAmountsAsync()
        {
            return await OnGetAmountsAsync();
        }


        /// <summary>
        /// Get amounts available to trade, symbol / amount dictionary
        /// </summary>
        /// <returns>Symbol / amount dictionary</returns>
        public virtual async Task<Dictionary<string, decimal>> GetAmountsAvailableToTradeAsync()
        {
            return  await OnGetAmountsAvailableToTradeAsync();
        }

        /// <summary>
        /// Place an order
        /// </summary>
        /// <param name="order">The order request</param>
        /// <returns>Result</returns>
        public virtual async Task<ExchangeOrderResult> PlaceOrderAsync(ExchangeOrderRequest order)
        {
            // *NOTE* do not wrap in CacheMethodCall
            await new SynchronizationContextRemover();
            order.MarketSymbol = NormalizeMarketSymbol(order.MarketSymbol);
            return await OnPlaceOrderAsync(order);
        }

        /// <summary>
        /// Place bulk orders
        /// </summary>
        /// <param name="orders">Order requests</param>f
        /// <returns>Order results, each result matches up with each order in index</returns>
        public virtual async Task<ExchangeOrderResult[]> PlaceOrdersAsync(params ExchangeOrderRequest[] orders)
        {
            // *NOTE* do not wrap in CacheMethodCall
            await new SynchronizationContextRemover();
            foreach (ExchangeOrderRequest request in orders)
            {
                request.MarketSymbol = NormalizeMarketSymbol(request.MarketSymbol);
            }
            return await OnPlaceOrdersAsync(orders);
        }

        /// <summary>
        /// Get order details
        /// </summary>
        /// <param name="orderId">Order id to get details for</param>
        /// <param name="marketSymbol">Symbol of order (most exchanges do not require this)</param>
        /// <returns>Order details</returns>
        public virtual async Task<ExchangeOrderResult> GetOrderDetailsAsync(string orderId, string marketSymbol = null)
        {
            marketSymbol = NormalizeMarketSymbol(marketSymbol);
            return await OnGetOrderDetailsAsync(orderId, marketSymbol);
        }

        /// <summary>
        /// Get the details of all open orders
        /// </summary>
        /// <param name="marketSymbol">Symbol to get open orders for or null for all</param>
        /// <returns>All open order details</returns>
        public virtual async Task<IEnumerable<ExchangeOrderResult>> GetOpenOrderDetailsAsync(string marketSymbol = null)
        {
            marketSymbol = NormalizeMarketSymbol(marketSymbol);
            return await OnGetOpenOrderDetailsAsync(marketSymbol);
        }

        /// <summary>
        /// Get the details of all completed orders
        /// </summary>
        /// <param name="marketSymbol">Symbol to get completed orders for or null for all</param>
        /// <param name="afterDate">Only returns orders on or after the specified date/time</param>
        /// <returns>All completed order details for the specified symbol, or all if null symbol</returns>
        public virtual async Task<IEnumerable<ExchangeOrderResult>> GetCompletedOrderDetailsAsync(string marketSymbol = null, DateTime? afterDate = null)
        {
            marketSymbol = NormalizeMarketSymbol(marketSymbol);
            return await OnGetCompletedOrderDetailsAsync(marketSymbol, afterDate);
        }

        /// <summary>
        /// Cancel an order, an exception is thrown if error
        /// </summary>
        /// <param name="orderId">Order id of the order to cancel</param>
        /// <param name="marketSymbol">Symbol of order (most exchanges do not require this)</param>
        public virtual async Task CancelOrderAsync(string orderId, string marketSymbol = null)
        {
            // *NOTE* do not wrap in CacheMethodCall
            await new SynchronizationContextRemover();
            await OnCancelOrderAsync(orderId, NormalizeMarketSymbol(marketSymbol));
        }

        /// <summary>
        /// Asynchronous withdraws request.
        /// </summary>
        /// <param name="withdrawalRequest">The withdrawal request.</param>
        public virtual async Task<ExchangeWithdrawalResponse> WithdrawAsync(ExchangeWithdrawalRequest withdrawalRequest)
        {
            // *NOTE* do not wrap in CacheMethodCall
            await new SynchronizationContextRemover();
            withdrawalRequest.Currency = NormalizeMarketSymbol(withdrawalRequest.Currency);
            return await OnWithdrawAsync(withdrawalRequest);
        }

        /// <summary>
        /// Get margin amounts available to trade, symbol / amount dictionary
        /// </summary>
        /// <param name="includeZeroBalances">Include currencies with zero balance in return value</param>
        /// <returns>Symbol / amount dictionary</returns>
        public virtual async Task<Dictionary<string, decimal>> GetMarginAmountsAvailableToTradeAsync(bool includeZeroBalances = false)
        {
            return  await OnGetMarginAmountsAvailableToTradeAsync(includeZeroBalances);
        }

        /// <summary>
        /// Get open margin position
        /// </summary>
        /// <param name="marketSymbol">Symbol</param>
        /// <returns>Open margin position result</returns>
        public virtual async Task<ExchangeMarginPositionResult> GetOpenPositionAsync(string marketSymbol)
        {
            marketSymbol = NormalizeMarketSymbol(marketSymbol);
            return  await OnGetOpenPositionAsync(marketSymbol);
        }

        /// <summary>
        /// Close a margin position
        /// </summary>
        /// <param name="marketSymbol">Symbol</param>
        /// <returns>Close margin position result</returns>
        public virtual async Task<ExchangeCloseMarginPositionResult> CloseMarginPositionAsync(string marketSymbol)
        {
            // *NOTE* do not wrap in CacheMethodCall
            await new SynchronizationContextRemover();
            return await OnCloseMarginPositionAsync(NormalizeMarketSymbol(marketSymbol));
        }

        #endregion REST API

        #region Web Socket API

        /// <summary>
        /// Get all tickers via web socket
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <param name="symbols"></param>
        /// <returns>Web socket, call Dispose to close</returns>
        public virtual IWebSocket GetTickersWebSocket(Action<IReadOnlyCollection<KeyValuePair<string, ExchangeTicker>>> callback, params string[] symbols)
        {
            callback.ThrowIfNull(nameof(callback), "Callback must not be null");
            return OnGetTickersWebSocket(callback, symbols);
        }

        /// <summary>
        /// Get information about trades via web socket
        /// </summary>
        /// <param name="callback">Callback (symbol and trade)</param>
        /// <param name="marketSymbols">Market Symbols</param>
        /// <returns>Web socket, call Dispose to close</returns>
        public virtual IWebSocket GetTradesWebSocket(Action<KeyValuePair<string, ExchangeTrade>> callback, params string[] marketSymbols)
        {
            callback.ThrowIfNull(nameof(callback), "Callback must not be null");
            return OnGetTradesWebSocket(callback, marketSymbols);
        }

        /// <summary>
        /// Get delta order book bids and asks via web socket. Only the deltas are returned for each callback. To manage a full order book, use ExchangeAPIExtensions.GetDepthWebSocket.
        /// </summary>
        /// <param name="callback">Callback of symbol, order book</param>
        /// <param name="maxCount">Max count of bids and asks - not all exchanges will honor this parameter</param>
        /// <param name="marketSymbols">Market symbols or null/empty for all of them (if supported)</param>
        /// <returns>Web socket, call Dispose to close</returns>
        public virtual IWebSocket GetDepthWebSocket(Action<ExchangeOrderBook> callback, int maxCount = 20, params string[] marketSymbols)
        {
            callback.ThrowIfNull(nameof(callback), "Callback must not be null");
            return OnGetOrderBookWebSocket(callback, maxCount, marketSymbols);
        }

        /// <summary>
        /// Get the details of all changed orders via web socket
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <returns>Web socket, call Dispose to close</returns>
        public virtual IWebSocket GetOrderDetailsWebSocket(Action<ExchangeOrderResult> callback)
        {
            callback.ThrowIfNull(nameof(callback), "Callback must not be null");
            return OnGetOrderDetailsWebSocket(callback);
        }

        /// <summary>
        /// Get the details of all completed orders via web socket
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <returns>Web socket, call Dispose to close</returns>
        public virtual IWebSocket GetCompletedOrderDetailsWebSocket(Action<ExchangeOrderResult> callback)
        {
            callback.ThrowIfNull(nameof(callback), "Callback must not be null");
            return OnGetCompletedOrderDetailsWebSocket(callback);
        }

        #endregion Web Socket API
    }
}
