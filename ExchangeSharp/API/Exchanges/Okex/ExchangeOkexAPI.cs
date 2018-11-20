using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Centipede
{
    [ExchangeMeta("Okex")]
    public sealed partial class ExchangeOkexAPI : ExchangeAPI
    {
        public override string BaseUrl { get; set; } = "https://www.okex.com/api/v1";
        public string BaseUrlV2 { get; set; } = "https://www.okex.com/v2/spot";
        public override string BaseUrlWebSocket { get; set; } = "wss://real.okex.com:10441/websocket?compress=true";

	/// <summary>
	/// China time to utc, no DST correction needed
	/// </summary>
	private static readonly TimeSpan ChinaTimeOffset = TimeSpan.FromHours(-8);
		
        public ExchangeOkexAPI()
        {
            RequestContentType = "application/x-www-form-urlencoded";
            WebSocketDepthType = WebSocketDepthType.FullBookFirstThenDeltas;
        }


        private string GetPayloadForm(Dictionary<string, object> payload)
        {
            payload["api_key"] = PublicApiKey.ToUnsecureString();
            string form = payload.GetFormForPayload(false);
            string sign = form + "&secret_key=" + PrivateApiKey.ToUnsecureString();
            sign = CryptoUtility.MD5Sign(sign);
            return form + "&sign=" + sign;
        }

        private string GetAuthForWebSocket()
        {
            string apiKey = PublicApiKey.ToUnsecureString();
            string param = "api_key=" + apiKey + "&secret_key=" + PrivateApiKey.ToUnsecureString();
            string sign = CryptoUtility.MD5Sign(param);
            return $"{{ \"event\": \"login\", \"parameters\": {{ \"api_key\": \"{apiKey}\", \"sign\": \"{sign}\" }} }}";
        }

        #region ProcessRequest

        protected override JToken CheckJsonResponse(JToken result)
        {
            if (result is JArray)
            {
                return result;
            }
            JToken innerResult = result["result"];
            if (innerResult != null && !innerResult.ConvertInvariant<bool>())
            {
                throw new APIException("Result is false: " + result.ToString());
            }
            innerResult = result["code"];
            if (innerResult != null && innerResult.ConvertInvariant<int>() != 0)
            {
                throw new APIException("Code is non-zero: " + result.ToString());
            }
            return result["data"] ?? result;
        }

        protected override async Task ProcessRequestAsync(IHttpWebRequest request, Dictionary<string, object> payload)
        {
            if (CanMakeAuthenticatedRequest(payload))
            {
                string msg = GetPayloadForm(payload);
                await CryptoUtility.WriteToRequestAsync(request, msg);
            }
        }

        private async Task<Tuple<JToken, string>> MakeRequestOkexAsync(string marketSymbol, string subUrl, string baseUrl = null)
        {
            JToken obj = await MakeJsonRequestAsync<JToken>(subUrl.Replace("$SYMBOL$", marketSymbol ?? string.Empty), baseUrl);
            return new Tuple<JToken, string>(obj, marketSymbol);
        }
        #endregion

        #region Public APIs

   

        public override Task<List<Currency>> GetCurrenciesAsync()
        {
            throw new NotImplementedException();
        }

        public override async Task<List<Symbol>> GetSymbolsAsync()
        {
            /*
             {"code":0,"data":[{"baseCurrency":1,"collect":"0","isMarginOpen":false,"listDisplay": 0,
    "marginRiskPreRatio": 0,
    "marginRiskRatio": 0,
    "marketFrom": 103,
    "maxMarginLeverage": 0,
    "maxPriceDigit": 8,
    "maxSizeDigit": 6,
    "minTradeSize": 0.00100000,
    "online": 1,
    "productId": 12,
    "quoteCurrency": 0,
    "quoteIncrement": 1E-8,
    "quotePrecision": 4,
    "sort": 10013,
    "symbol": "ltc_btc"
},
             */
            List<Symbol> markets = new List<Symbol>();
            JToken allMarketSymbolTokens = await MakeJsonRequestAsync<JToken>("/markets/products", BaseUrlV2);
            foreach (JToken marketSymbolToken in allMarketSymbolTokens)
            {
                var marketName = marketSymbolToken["symbol"].ToStringInvariant();
                string[] pieces = marketName.ToStringUpperInvariant().Split('_');
                var market = new Symbol
                {
                    OriginSymbol = marketName,
                    IsActive = marketSymbolToken["online"].ConvertInvariant<bool>(),
                    //QuoteCurrency = pieces[1],
                    //BaseCurrency = pieces[0],
                    MarginEnabled = marketSymbolToken["isMarginOpen"].ConvertInvariant(false)
                };

                var quotePrecision = marketSymbolToken["quotePrecision"].ConvertInvariant<double>();
                var quantityStepSize = Math.Pow(10, -quotePrecision);
                market.QuantityStepSize = quantityStepSize.ConvertInvariant<decimal>();
                var maxSizeDigit = marketSymbolToken["maxSizeDigit"].ConvertInvariant<double>();
                var maxTradeSize = Math.Pow(10, maxSizeDigit);
                market.MaxTradeSize = maxTradeSize.ConvertInvariant<decimal>() - 1.0m;
                market.MinTradeSize = marketSymbolToken["minTradeSize"].ConvertInvariant<decimal>();

                market.PriceStepSize = marketSymbolToken["quoteIncrement"].ConvertInvariant<decimal>();
                market.MinPrice = market.PriceStepSize.Value;
                var maxPriceDigit = marketSymbolToken["maxPriceDigit"].ConvertInvariant<double>();
                var maxPrice = Math.Pow(10, maxPriceDigit);
                market.MaxPrice = maxPrice.ConvertInvariant<decimal>() - 1.0m;

                markets.Add(market);
            }
            return markets;
        }

        public override async Task<ExchangeTicker> GetTickerAsync(Symbol symbol)
        {
            var data = await MakeRequestOkexAsync(symbol.OriginSymbol, "/ticker.do?symbol=$SYMBOL$");
            //return ParseTicker(data.Item2, data.Item1); todo

            return null;
        }

        public override async Task<List<ExchangeTicker>> GetTickersAsync()
        {
            var data = await MakeRequestOkexAsync(null, "/markets/index-tickers?limit=100000000", BaseUrlV2);
            List<ExchangeTicker> tickers = new List<ExchangeTicker>();
            foreach (JToken token in data.Item1)
            {
                var marketSymbol = token["symbol"].ToStringInvariant();
                //tickers.Add(ParseTickerV2(marketSymbol, token)); todo:改到这里在处理
            }
            return tickers;
        }


        public override Task<List<ExchangeTrade>> GetTradesAsync(Symbol symbol,int limit)
        {
            throw new NotImplementedException();
        }

        public override IWebSocket GetTradesWebSocket(Action<List<ExchangeTrade>> callback, params Symbol[] symbols)
        {
            /*
            {[
              {
                "binary": 0,
                "channel": "addChannel",
                "data": {
                  "result": true,
                  "channel": "ok_sub_spot_btc_usdt_deals"
                }
              }
            ]}
            {[
              {
                "binary": 0,
                "channel": "ok_sub_spot_btc_usdt_deals",
                "data": [
                  [
                    "335599480",
                    "7396",
                    "0.0031002",
                    "20:23:51",
                    "bid"
                  ],
                  [
                    "335599497",
                    "7395.9153",
                    "0.0031",
                    "20:23:51",
                    "bid"
                  ],
                  [
                    "335599499",
                    "7395.7889",
                    "0.00409436",
                    "20:23:51",
                    "ask"
                  ],

                ]
              }
            ]}
            */

            //return ConnectWebSocketOkex(async (_socket) =>
            //{
            //    symbols = await AddMarketSymbolsToChannel(_socket, "ok_sub_spot_{0}_deals", symbols);
            //}, (_socket, symbol, sArray, token) =>
            //{
            //    List<ExchangeTrade> trades = ParseTradesWebSocket(token);
            //    foreach (var trade in trades)
            //    {
            //        callback(new KeyValuePair<string, ExchangeTrade>(symbol, trade));
            //    }
            //    return Task.CompletedTask;
            //});

            return null;
        }

        public override IWebSocket GetDepthWebSocket(Action<ExchangeDepth> callback, int maxCount = 20, params Symbol[] symbols)
        {
            /*
{[
  {
    "binary": 0,
    "channel": "addChannel",
    "data": {
      "result": true,
      "channel": "ok_sub_spot_bch_btc_depth_5"
    }
  }
]}

{[
  {
    "data": {
      "asks": [
        [
          "8364.1163",
          "0.005"
        ],
  
      ],
      "bids": [
        [
          "8335.99",
          "0.01837999"
        ],
        [
          "8335.9899",
          "0.06"
        ],
      ],
      "timestamp": 1526734386064
    },
    "binary": 0,
    "channel": "ok_sub_spot_btc_usdt_depth_20"
  }
]}
                 
                 */

            return ConnectWebSocketOkex(async (_socket) =>
            {
               //todo symbols = await AddMarketSymbolsToChannel(_socket, $"ok_sub_spot_{{0}}_depth_{maxCount}", symbols);
            }, (_socket, symbol, sArray, token) =>
            {
                ExchangeDepth book = token.ParseDepthFromJTokenArrays(null, sequence: "timestamp", maxCount: maxCount); //todo:改为symbol
                callback(book);
                return Task.CompletedTask;
            });
        }

        public override async Task<ExchangeDepth> GetDepthAsync(Symbol symbol, int maxCount)
        {
            var token = await MakeRequestOkexAsync(symbol.OriginSymbol, "/depth.do?symbol=$SYMBOL$");
            return token.Item1.ParseDepthFromJTokenArrays(symbol, maxCount: maxCount);
        }

        //public override async Task GetHistoricalTradesAsync(Func<List<ExchangeTrade>, bool> callback, Symbol symbol , DateTime? startDate = null, DateTime? endDate = null)
        //{
        //    List<ExchangeTrade> allTrades = new List<ExchangeTrade>();
        //    var trades = await MakeRequestOkexAsync(symbol.OriginSymbol, "/trades.do?symbol=$SYMBOL$");
        //    foreach (JToken trade in trades.Item1)
        //    {
        //        // [ { "date": "1367130137", "date_ms": "1367130137000", "price": 787.71, "amount": 0.003, "tid": "230433", "type": "sell" } ]
        //        allTrades.Add(trade.ParseTrade("amount", "price", "type", "date_ms", TimestampType.UnixMilliseconds, "tid"));
        //    }
        //    callback(allTrades);
        //}

        public override async Task<List<MarketCandle>> GetCandlesAsync(Symbol symbol, int periodSeconds,
            DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
        {
            /*
             [
	            1417564800000,	timestamp
	            384.47,		open
	            387.13,		high
	            383.5,		low
	            387.13,		close
	            1062.04,	volume
            ]
            */

            List<MarketCandle> candles = new List<MarketCandle>();
            string url = "/kline.do?symbol=" + symbol.OriginSymbol;
            if (startDate != null)
            {
                url += "&since=" + (long)startDate.Value.UnixTimestampFromDateTimeMilliseconds();
            }
            if (limit != null)
            {
                url += "&size=" + (limit.Value.ToStringInvariant());
            }
            string periodString = CryptoUtility.SecondsToPeriodStringLong(periodSeconds);
            url += "&type=" + periodString;

            JToken obj = await MakeJsonRequestAsync<JToken>(url);

            foreach (JArray token in obj)
            {
                //todo:这里转换方案好像有问题了，不一定能支持的了，不行就改为object
               // candles.Add(token.ParseCandle(symbol, periodSeconds, 1, 2, 3, 4, 0, TimestampType.UnixMilliseconds, 5));
            }
            return candles;
        }

        #endregion

        #region Private APIs
        protected override async Task<Dictionary<string, decimal>> OnGetAmountsAsync()
        {
            /*
             {
  "result": true,
  "info": {
    "funds": {
      "borrow": {
        "ssc": "0",
                
                
        "xlm": "0",
        "swftc": "0",
        "hmc": "0"
      },
      "free": {
        "ssc": "0",

        "swftc": "0",
        "hmc": "0"
      },
      "freezed": {
        "ssc": "0",
                
        "xlm": "0",
        "swftc": "0",
        "hmc": "0"
      }
    }
  }
}
             */
            Dictionary<string, decimal> amounts = new Dictionary<string, decimal>();
            var payload = await GetNoncePayloadAsync();
            JToken token = await MakeJsonRequestAsync<JToken>("/userinfo.do", BaseUrl, payload, "POST");
            var funds = token["info"]["funds"];

            foreach (JProperty fund in funds)
            {
                ParseAmounts(fund.Value, amounts);
            }
            return amounts;
        }

        protected override async Task<Dictionary<string, decimal>> OnGetAmountsAvailableToTradeAsync()
        {
            Dictionary<string, decimal> amounts = new Dictionary<string, decimal>();
            var payload = await GetNoncePayloadAsync();
            JToken token = await MakeJsonRequestAsync<JToken>("/userinfo.do", BaseUrl, payload, "POST");
            var funds = token["info"]["funds"];
            var free = funds["free"];

            return ParseAmounts(funds["free"], amounts);
        }

        public override async Task<ExchangeOrderResult> PlaceOrderAsync(ExchangeOrderRequest order)
        {
            Dictionary<string, object> payload = await GetNoncePayloadAsync();
            payload["symbol"] = order.Symbol.OriginSymbol;
            payload["type"] = (order.IsBuy ? "buy" : "sell");

            decimal outputQuantity =  ClampOrderQuantity(order.Symbol, order.Amount);
            decimal outputPrice =  ClampOrderPrice(order.Symbol, order.Price);

            if (order.OrderType == OrderType.Market)
            {
                // TODO: Fix later once Okex fixes this on their end
                throw new NotSupportedException("Okex confuses price with amount while sending a market order, so market orders are disabled for now");

                /*
                payload["type"] += "_market";
                if (order.IsBuy)
                {
                    // for market buy orders, the price is to total amount you want to buy, 
                    // and it must be higher than the current price of 0.01 BTC (minimum buying unit), 0.1 LTC or 0.01 ETH
                    payload["price"] = outputQuantity;
                }
                else
                {
                    // For market buy roders, the amount is not required
                    payload["amount"] = outputQuantity;
                }
                */
            }
            else
            {
                payload["price"] = outputPrice;
                payload["amount"] = outputQuantity;
            }
            order.ExtraParameters.CopyTo(payload);

            JToken obj = await MakeJsonRequestAsync<JToken>("/trade.do", BaseUrl, payload, "POST");
            order.Amount = outputQuantity;
            order.Price = outputPrice;
            return ParsePlaceOrder(obj, order);
        }

        public override async Task CancelOrderAsync(string orderId, Symbol symbol = null)
        {
            Dictionary<string, object> payload = await GetNoncePayloadAsync();

            payload["symbol"] = symbol.OriginSymbol;
            payload["order_id"] = orderId;
            await MakeJsonRequestAsync<JToken>("/cancel_order.do", BaseUrl, payload, "POST");
        }

        public override async Task<ExchangeOrderResult> GetOrderDetailsAsync(string orderId,Symbol symbol = null)
        {
            List<ExchangeOrderResult> orders = new List<ExchangeOrderResult>();
            Dictionary<string, object> payload = await GetNoncePayloadAsync();
        
            payload["symbol"] = symbol.OriginSymbol;
            payload["order_id"] = orderId;
            JToken token = await MakeJsonRequestAsync<JToken>("/order_info.do", BaseUrl, payload, "POST");
            foreach (JToken order in token["orders"])
            {
                orders.Add(ParseOrder(order));
            }

            // only return the first
            return orders[0];
        }

        protected override async Task<IEnumerable<ExchangeOrderResult>> OnGetOpenOrderDetailsAsync(string marketSymbol)
        {
            List<ExchangeOrderResult> orders = new List<ExchangeOrderResult>();
            Dictionary<string, object> payload = await GetNoncePayloadAsync();

            payload["symbol"] = marketSymbol;
            // if order_id is -1, then return all unfilled orders, otherwise return the order specified
            payload["order_id"] = -1;
            JToken token = await MakeJsonRequestAsync<JToken>("/order_info.do", BaseUrl, payload, "POST");
            foreach (JToken order in token["orders"])
            {
                orders.Add(ParseOrder(order));
            }

            return orders;
        }

        public override IWebSocket GetTickerWebSocket(Action<ExchangeTicker> callback, params Symbol[] symbols)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private Functions

        //private ExchangeTicker ParseTicker(string symbol, JToken data)
        //{
        //    //{"date":"1518043621","ticker":{"high":"0.01878000","vol":"1911074.97335534","last":"0.01817627","low":"0.01813515","buy":"0.01817626","sell":"0.01823447"}}
        //    return this.ParseTicker(data["ticker"], symbol, "sell", "buy", "last", "vol", null, "date", TimestampType.UnixSeconds);
        //}

        //private ExchangeTicker ParseTickerV2(string symbol, JToken ticker)
        //{
        //    // {"buy":"0.00001273","change":"-0.00000009","changePercentage":"-0.70%","close":"0.00001273","createdDate":1527355333053,"currencyId":535,"dayHigh":"0.00001410","dayLow":"0.00001174","high":"0.00001410","inflows":"19.52673814","last":"0.00001273","low":"0.00001174","marketFrom":635,"name":{},"open":"0.00001282","outflows":"52.53715678","productId":535,"sell":"0.00001284","symbol":"you_btc","volume":"5643177.15601228"}
        //    return this.ParseTicker(ticker, symbol, "sell", "buy", "last", "volume", null, "createdDate", TimestampType.UnixMilliseconds);
        //}

        private Dictionary<string, decimal> ParseAmounts(JToken token, Dictionary<string, decimal> amounts)
        {
            foreach (JProperty prop in token)
            {
                var amount = prop.Value.ConvertInvariant<decimal>();
                if (amount == 0m)
                    continue;

                if (amounts.ContainsKey(prop.Name))
                {
                    amounts[prop.Name] += amount;
                }
                else
                {
                    amounts[prop.Name] = amount;
                }
            }
            return amounts;
        }

        private ExchangeOrderResult ParsePlaceOrder(JToken token, ExchangeOrderRequest order)
        {
            /*
              {"result":true,"order_id":123456}
            */
            ExchangeOrderResult result = new ExchangeOrderResult
            {
                Amount = order.Amount,
                Price = order.Price,
                IsBuy = order.IsBuy,
                OrderId = token["order_id"].ToStringInvariant(),
                MarketSymbol = order.Symbol.OriginSymbol
            };
            result.AveragePrice = result.Price;
            result.Result = ExchangeAPIOrderResult.Pending;

            return result;
        }

        private ExchangeAPIOrderResult ParseOrderStatus(int status)
        {
            // status: -1 = cancelled, 0 = unfilled, 1 = partially filled, 2 = fully filled, 3 = cancel request in process
            switch (status)
            {
                case -1:
                    return ExchangeAPIOrderResult.Canceled;
                case 0:
                    return ExchangeAPIOrderResult.Pending;
                case 1:
                    return ExchangeAPIOrderResult.FilledPartially;
                case 2:
                    return ExchangeAPIOrderResult.Filled;
                case 3:
                    return ExchangeAPIOrderResult.PendingCancel;
                case 4:
                    return ExchangeAPIOrderResult.Error;
                default:
                    return ExchangeAPIOrderResult.Unknown;
            }
        }

        private ExchangeOrderResult ParseOrder(JToken token)
        {
            /*
            {
    "result": true,
    "orders": [
        {
            "amount": 0.1,
            "avg_price": 0,
            "create_date": 1418008467000,
            "deal_amount": 0,
            "order_id": 10000591,
            "orders_id": 10000591,
            "price": 500,
            "status": 0,
            "symbol": "btc_usd",
            "type": "sell"
        },
            */
            ExchangeOrderResult result = new ExchangeOrderResult
            {
                Amount = token["amount"].ConvertInvariant<decimal>(),
                AmountFilled = token["deal_amount"].ConvertInvariant<decimal>(),
                Price = token["price"].ConvertInvariant<decimal>(),
                AveragePrice = token["avg_price"].ConvertInvariant<decimal>(),
                IsBuy = token["type"].ToStringInvariant().StartsWith("buy"),
                OrderDate = CryptoUtility.UnixTimeStampToDateTimeMilliseconds(token["create_date"].ConvertInvariant<long>()),
                OrderId = token["order_id"].ToStringInvariant(),
                MarketSymbol = token["symbol"].ToStringInvariant(),
                Result = ParseOrderStatus(token["status"].ConvertInvariant<int>()),
            };

            return result;
        }

        private List<ExchangeTrade> ParseTradesWebSocket(JToken token)
        {
            var trades = new List<ExchangeTrade>();
            foreach (var t in token)
            {
                var ts = TimeSpan.Parse(t[3].ToStringInvariant()) + ChinaTimeOffset;
                if (ts < TimeSpan.FromHours(0)) ts += TimeSpan.FromHours(24);
                var dt = CryptoUtility.UtcNow.Date.Add(ts);
                var trade = new ExchangeTrade()
                {
                    Id = t[0].ToStringInvariant(),
                    Price = t[1].ConvertInvariant<decimal>(),
                    Amount = t[2].ConvertInvariant<decimal>(),
                    Timestamp = dt,  //todo: 这里处理方式有问题
                    IsBuy = t[4].ToStringInvariant().EqualsWithOption("bid"),
                };
                trades.Add(trade);
            }

            return trades;
        }

        private IWebSocket ConnectWebSocketOkex(Func<IWebSocket, Task> connected, Func<IWebSocket, string, string[], JToken, Task> callback, int symbolArrayIndex = 3)
        {
            return ConnectWebSocket(string.Empty, async (_socket, msg) =>
            {
                // https://github.com/okcoin-okex/API-docs-OKEx.com/blob/master/README-en.md
                // All the messages returning from WebSocket API will be optimized by Deflate compression
                JToken token = JToken.Parse(msg.ToStringFromUTF8Deflate());
                token = token[0];
                var channel = token["channel"].ToStringInvariant();
                if (channel.EqualsWithOption("addChannel"))
                {
                    return;
                }
                else if (channel.EqualsWithOption("login"))
                {
                    if (token["data"] != null && token["data"]["result"] != null && token["data"]["result"].ConvertInvariant<bool>())
                    {
                        await callback(_socket, "login", null, null);
                    }
                }
                else
                {
                    var sArray = channel.Split('_');
                    string marketSymbol = null; // todo sArray[symbolArrayIndex] + MarketSymbolSeparator + sArray[symbolArrayIndex + 1];
                    await callback(_socket, marketSymbol, sArray, token["data"]);
                }
            }, async (_socket) =>
            {
                await connected(_socket);
            });
        }

        private IWebSocket ConnectPrivateWebSocketOkex(Func<IWebSocket, Task> connected, Func<IWebSocket, string, string[], JToken, Task> callback, int symbolArrayIndex = 3)
        {
            return ConnectWebSocketOkex(async (_socket) =>
            {
                await _socket.SendMessageAsync(GetAuthForWebSocket());
            }, async (_socket, symbol, sArray, token) =>
            {
                if (symbol == "login")
                {
                    await connected(_socket);
                }
                else
                {
                    await callback(_socket, symbol, sArray, token);
                }
            }, 0);
        }

        private async Task<string[]> AddMarketSymbolsToChannel(IWebSocket socket, string channelFormat, string[] marketSymbols, bool useJustFirstSymbol = false)
        {
            //if (marketSymbols == null || marketSymbols.Length == 0)
            //{
            //    marketSymbols = null; //todo (await GetMarketSymbolsAsync()).ToArray();
            //}
            //foreach (string marketSymbol in marketSymbols)
            //{
            //    string normalizedSymbol = NormalizeMarketSymbol(marketSymbol);
            //    if (useJustFirstSymbol)
            //    {
            //        normalizedSymbol = normalizedSymbol.Substring(0, normalizedSymbol.IndexOf(MarketSymbolSeparator[0]));
            //    }
            //    string channel = string.Format(channelFormat, normalizedSymbol);
            //    await socket.SendMessageAsync(new { @event = "addChannel", channel });
            //}
            //return marketSymbols;

            return null;
        }


        public override IWebSocket GetCandlesWebSocket(Action<MarketCandle> callback, int periodSeconds, Symbol[] symbols)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
    
}
