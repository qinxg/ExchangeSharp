using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Centipede
{
    [ExchangeMeta("Huobi")]
    public sealed partial class ExchangeHuobiAPI : ExchangeAPI
    {
        public override string BaseUrl { get; set; } = "https://api.huobipro.com";
        public string BaseUrlV1 { get; set; } = "https://api.huobipro.com/v1";
        public override string BaseUrlWebSocket { get; set; } = "wss://api.huobipro.com/ws";

        private long _webSocketId = 0;

        public ExchangeHuobiAPI()
        {
            RequestContentType = "application/x-www-form-urlencoded";
            NonceStyle = NonceStyle.UnixMilliseconds;
            MarketSymbolSeparator = string.Empty;
            MarketSymbolIsUppercase = false;
            WebSocketDepthType = WebSocketDepthType.FullBookAlways;
        }

        #region HTTP请求处理 

        protected override async Task ProcessRequestAsync(IHttpWebRequest request, Dictionary<string, object> payload)
        {
            if (CanMakeAuthenticatedRequest(payload))
            {
                if (request.Method == "POST")
                {
                    request.AddHeader("content-type", "application/json");
                    payload.Remove("nonce"); //移除随机数
                    var msg = payload.GetJsonForPayload();
                    await request.WriteToRequestAsync(msg);
                }
            }
        }

        protected override Uri ProcessRequestUrl(UriBuilder url, Dictionary<string, object> payload, string method)
        {
            if (CanMakeAuthenticatedRequest(payload))
            {
                /*
                 * 基于安全考虑，除行情API 外的 API 请求都必须进行签名运算。一个合法的请求由以下几部分组成：
                   方法请求地址 即访问服务器地址：api.huobi.pro，api.hadax.com或者api.dm.huobi.br.com后面跟上方法名，比如api.huobi.pro/v1/order/orders。
                   API 访问密钥（AccessKeyId） 您申请的 APIKEY 中的AccessKey。
                   签名方法（SignatureMethod） 用户计算签名的基于哈希的协议，此处使用 HmacSHA256。
                   签名版本（SignatureVersion） 签名协议的版本，此处使用2。
                   时间戳（Timestamp） 您发出请求的时间 (UTC 时区) (UTC 时区) (UTC 时区) 。在查询请求中包含此值有助于防止第三方截取您的请求。如：2017-05-11T16:22:06。再次强调是 (UTC 时区) 。
                   必选和可选参数 每个方法都有一组用于定义 API 调用的必需参数和可选参数。可以在每个方法的说明中查看这些参数及其含义。 请一定注意：对于GET请求，每个方法自带的参数都需要进行签名运算； 对于POST请求，每个方法自带的参数不进行签名认证，即POST请求中需要进行签名运算的只有AccessKeyId、SignatureMethod、SignatureVersion、Timestamp四个参数，其它参数放在body中。
                   签名 签名计算得出的值，用于确保签名有效和未被篡改。
                 */


                // must sort case sensitive
                var dict = new SortedDictionary<string, object>(StringComparer.Ordinal)
                {
                    ["Timestamp"] =
                        CryptoUtility.UnixTimeStampToDateTimeMilliseconds(payload["nonce"].ConvertInvariant<long>())
                            .ToString("s"),
                    //这里的逻辑是生成一个随机数，然后把这个随机数转成正常的日期。然后转成UTC时间 带T的那种 。有点脱了裤子放屁的感觉。

                    ["AccessKeyId"] = PublicApiKey.ToUnsecureString(),
                    ["SignatureMethod"] = "HmacSHA256",
                    ["SignatureVersion"] = "2"
                };

                if (method == "GET") //只有get需要验证参数内的内容
                {
                    foreach (var kv in payload)
                    {
                        dict.Add(kv.Key, kv.Value);
                    }
                }

                string msg = dict.GetFormForPayload(false, false, false);
                string toSign = $"{method}\n{url.Host}\n{url.Path}\n{msg}";

                // calculate signature
                var sign = CryptoUtility.SHA256SignBase64(toSign, PrivateApiKey.ToUnsecureBytesUTF8()).UrlEncode();

                // append signature to end of message
                msg += $"&Signature={sign}";

                url.Query = msg;
            }

            return url.Uri;
        }

        #endregion

        #region common

        public override async Task<List<Currency>> GetCurrenciesAsync()
        {
            /*
             * {"status":"ok","data":["hb10","usdt","btc","bch","eth","xrp","ltc","ht","ada","eos","iota","xem","xmr","dash","neo","trx","icx","lsk","qtum","etc","btg","omg","hc","zec","dcr","steem","bts","waves","snt","salt","gnt","cmt","btm","pay","knc","powr","bat","dgd","ven","qash","zrx","gas","mana","eng","cvc","mco","mtl","rdn","storj","chat","srn","link","act","tnb","qsp","req","phx","appc","rcn","smt","adx","tnt","ost","itc","lun","gnx","ast","evx","mds","snc","propy","eko","nas","bcd","wax","wicc","topc","swftc","dbc","elf","aidoc","qun","iost","yee","dat","theta","let","dta","utk","meet","zil","soc","ruff","ocn","ela","bcx","sbtc","etf","bifi","zla","stk","wpr","mtn","mtx","edu","blz","abt","ont","ctxc","bft","wan","kan","lba","poly","pai","wtc","box","dgb","gxc","bix","xlm","xvg","hit","ong","bt1","bt2","xzc","vet","ncash","grs","egcc","she","mex","iic","gsc","uc","uip","cnn","aac","uuu","cdc","lxt","but","18c","datx","portal","gtc","hot","man","get","pc","ren","eosdac","ae","bkbt","gve","seele","fti","ekt","xmx","ycc","fair","ssp","eon","eop","lym","zjlt","meetone","pnt","idt","dac","bcv","sexc","tos","musk","add","mt","kcash","iq","ncc","rccc","hpt","cvcoin","rte","trio","ardr","nano","husd","zen","rbtc"]}
             */

            var currencies = new List<Currency>();
            var result = await MakeJsonRequestAsync<JToken>("/common/currencys", BaseUrlV1);

            foreach (var code in result)
            {
                var currency = new Currency
                {
                    OriginCurrency = code.ToStringInvariant(),
                    NormCurrency = code.ToStringLowerInvariant()
                };

                currencies.Add(currency);
            }

            return currencies;
        }


        public override async Task<List<Symbol>> GetSymbolsAsync()
        {
            /*
             {  "status": "ok", 
                "data":[{
                        "base-currency": "btc",
                        "quote-currency": "usdt",
                        "price-precision": 2,
                        "amount-precision": 4,
                        "symbol-partition": "main",
                        "symbol": "btcusdt"
                  }{
                        "base-currency": "eth",
                        "quote-currency": "usdt",
                        "price-precision": 2,
                        "amount-precision": 4,
                        "symbol-partition": "main",
                        "symbol": "ethusdt"
                  }]}
             */
            List<Symbol> markets = new List<Symbol>();
            JToken allMarketSymbols = await MakeJsonRequestAsync<JToken>("/common/symbols", BaseUrlV1);
            foreach (var marketSymbol in allMarketSymbols)
            {
                var baseCurrencyCode = marketSymbol["base-currency"].ToStringInvariant();
                var quoteCurrencyCode = marketSymbol["quote-currency"].ToStringInvariant();
                var baseCurrency = this.Currencies.FirstOrDefault(p => p.OriginCurrency == baseCurrencyCode);
                var quoteCurrency = this.Currencies.FirstOrDefault(p => p.OriginCurrency == quoteCurrencyCode);

                var originSymbol = marketSymbol["symbol"].ToStringInvariant();

                var pricePrecision = marketSymbol["price-precision"].ConvertInvariant<double>();
                var priceStepSize = Math.Pow(10, -pricePrecision).ConvertInvariant<decimal>();
                var amountPrecision = marketSymbol["amount-precision"].ConvertInvariant<double>();
                var quantityStepSize = Math.Pow(10, -amountPrecision).ConvertInvariant<decimal>();

                var market = new Symbol
                {
                    BaseCurrency = baseCurrency,
                    QuoteCurrency = quoteCurrency,
                    OriginSymbol = originSymbol,
                    IsActive = true,
                    PriceStepSize = priceStepSize,
                    QuantityStepSize = quantityStepSize,
                    MinPrice = priceStepSize,
                    MinTradeSize = quantityStepSize,
                };
                markets.Add(market);
            }

            return markets;
        }


        #endregion
        
        #region ticker


        public override async Task<ExchangeTicker> GetTickerAsync(Symbol symbol)
        {
            /*
             {{
              "status": "ok",
              "ch": "market.naseth.detail.merged",
              "ts": 1525136582460,
              "tick": {
                "amount": 1614089.3164448638,
                "open": 0.014552,
                "close": 0.013308,
                "high": 0.015145,
                "id": 6442118070,
                "count": 74643,
                "low": 0.013297,
                "version": 6442118070,
                "ask": [
                  0.013324,
                  0.0016
                ],
                "vol": 22839.223396720725,
                "bid": [
                  0.013297,
                  3192.2322
                ]
              }
            }}
             */

            JToken ticker = await MakeJsonRequestAsync<JToken>("/market/detail/merged?symbol=" + symbol.OriginSymbol);
            var data = ticker["tick"];
            return data.ParseTicker(symbol, "ask", "bid", "close", "amount", "vol", "ts",
                TimestampType.UnixMillisecondsDouble, idKey: "id");
        }

        public override async Task<List<ExchangeTicker>> GetTickersAsync()
        {
            List<ExchangeTicker> tickers = new List<ExchangeTicker>();

            JToken obj = await MakeJsonRequestAsync<JToken>("/market/tickers", BaseUrl);

            foreach (JToken child in obj)
            {
                tickers.Add(child.ParseTicker(null, null, "close", "amount", "vol"));
            }

            return tickers;
        }

        #endregion
        
        #region  trades


        public override async Task<List<ExchangeTrade>> GetTradesAsync(Symbol symbol, int limit = 20)
        {
            /*
             * {"status":"ok","ch":"market.ethusdt.trade.detail","ts":1542104543483,
             * "tick":{"id":27952707649,"ts":1542104541926,
             *  "data":[{"amount":0.100000000000000000,"ts":1542104541926,"id":2795270764916609236638,"price":210.900000000000000000,"direction":"buy"},{"amount":0.327700000000000000,"ts":1542104541926,"id":2795270764916609248055,"price":210.900000000000000000,"direction":"buy"}]}}
             */

            JToken result = await MakeJsonRequestAsync<JToken>($"/market/history/trade?symbol={symbol.OriginSymbol}&size={limit}", BaseUrl);
            var trades = ParseTradesData(result);
            return trades;
        }

        #endregion

        #region  Kline

        public override async Task<List<MarketCandle>> GetCandlesAsync(Symbol symbol,
            int periodSeconds, DateTime? startDate = null, DateTime? endDate = null, int? limit = null)
        {
            /*
            {
              "status": "ok",
              "ch": "market.btcusdt.kline.1day",
              "ts": 1499223904680,
              “data”: [
            {
                "id": 1499184000,
                "amount": 37593.0266,
                "count": 0,
                "open": 1935.2000,
                "close": 1879.0000,
                "low": 1856.0000,
                "high": 1940.0000,
                "vol": 71031537.97866500
              },
             */

            List<MarketCandle> candles = new List<MarketCandle>();
            string url = "/market/history/kline?symbol=" + symbol.OriginSymbol;

            if (limit != null)
            {
                // default is 150, max: 2000
                url += "&size=" + (limit.Value.ToStringInvariant());
            }

            string periodString = CryptoUtility.SecondsToPeriodStringLong(periodSeconds);
            url += "&period=" + periodString;

            JToken allCandles = await MakeJsonRequestAsync<JToken>(url, BaseUrl, null);
            foreach (var token in allCandles)
            {
                candles.Add(token.ParseCandle(symbol, periodSeconds, "open", "high", "low", "close", "id",
                    TimestampType.UnixSeconds, null, "vol"));
            }

            //todo:注意先插下k线的顺序
            candles.Reverse();
            return candles;
        }

        #endregion



        protected override IWebSocket OnGetTradesWebSocket(
            Action<KeyValuePair<string, ExchangeTrade>> callback,
            params string[] marketSymbols)
        {
            return ConnectWebSocket(string.Empty, async (socket, msg) =>
            {
                /*
{"id":"id1","status":"ok","subbed":"market.btcusdt.trade.detail","ts":1527574853489}
{{
  "ch": "market.btcusdt.trade.detail",
  "ts": 1527574905759,
  "tick": {
    "id": 8232977476,
    "ts": 1527574905623,
    "data": [
      {
        "amount": 0.3066,
        "ts": 1527574905623,
        "id": 82329774765058180723,
        "price": 7101.81,
        "direction": "buy"
      }
    ]
  }
}}
                 */
                var str = msg.ToStringFromUTF8Gzip();
                JToken token = JToken.Parse(str);

                if (token["status"] != null)
                {
                    return;
                }
                else if (token["ping"] != null)
                {
                    await socket.SendMessageAsync(str.Replace("ping", "pong"));
                    return;
                }

                var ch = token["ch"].ToStringInvariant();
                var sArray = ch.Split('.');
                var marketSymbol = sArray[1];

                var tick = token["tick"];
                var id = tick["id"].ToStringInvariant();

                var data = tick["data"];
                var trades = ParseTradesData(data);
                foreach (var trade in trades)
                {
                    trade.Id = id;
                    callback(new KeyValuePair<string, ExchangeTrade>(marketSymbol, trade));
                }
            }, async (_socket) =>
            {
                if (marketSymbols == null || marketSymbols.Length == 0)
                {
                    marketSymbols = null; //  todo: (await GetMarketSymbolsAsync()).ToArray();
                }

                foreach (string marketSymbol in marketSymbols)
                {
                    long id = System.Threading.Interlocked.Increment(ref _webSocketId);
                    string channel = $"market.{marketSymbol}.trade.detail";
                    await _socket.SendMessageAsync(new {sub = channel, id = "id" + id.ToStringInvariant()});
                }
            });
        }

        protected override IWebSocket OnGetOrderBookWebSocket(Action<ExchangeOrderBook> callback, int maxCount = 20,
            params string[] marketSymbols)
        {
            return ConnectWebSocket(string.Empty, async (_socket, msg) =>
            {
                /*
{{
  "id": "id1",
  "status": "ok",
  "subbed": "market.btcusdt.depth.step0",
  "ts": 1526749164133
}}
{{
  "ch": "market.btcusdt.depth.step0",
  "ts": 1526749254037,
  "tick": {
    "bids": [
      [
        8268.3,
        0.101
      ],
      [
        8268.29,
        0.8248
      ],
      
    ],
    "asks": [
      [
        8275.07,
        0.1961
      ],
	  
      [
        8337.1,
        0.5803
      ]
    ],
    "ts": 1526749254016,
    "version": 7664175145
  }
}}
                 */
                var str = msg.ToStringFromUTF8Gzip();
                JToken token = JToken.Parse(str);

                if (token["status"] != null)
                {
                    return;
                }
                else if (token["ping"] != null)
                {
                    await _socket.SendMessageAsync(str.Replace("ping", "pong"));
                    return;
                }

                var ch = token["ch"].ToStringInvariant();
                var sArray = ch.Split('.');
                var marketSymbol = sArray[1].ToStringInvariant();
                ExchangeOrderBook book =
                    ExchangeAPIExtensions.ParseOrderBookFromJTokenArrays(token["tick"], maxCount: maxCount);
                book.MarketSymbol = marketSymbol;
                callback(book);
            }, async (_socket) =>
            {
                if (marketSymbols == null || marketSymbols.Length == 0)
                {
                    marketSymbols = null; //todo (await GetMarketSymbolsAsync()).ToArray();
                }

                foreach (string symbol in marketSymbols)
                {
                    long id = System.Threading.Interlocked.Increment(ref _webSocketId);
                    var normalizedSymbol = NormalizeMarketSymbol(symbol);
                    string channel = $"market.{normalizedSymbol}.depth.step0";
                    await _socket.SendMessageAsync(new {sub = channel, id = "id" + id.ToStringInvariant()});
                }
            });
        }

        protected override async Task<ExchangeOrderBook> OnGetOrderBookAsync(string marketSymbol, int maxCount = 100)
        {
            /*
             {
  "status": "ok",
  "ch": "market.btcusdt.depth.step0",
  "ts": 1489472598812,
  "tick": {
    "id": 1489464585407,
    "ts": 1489464585407,
    "bids": [
      [7964, 0.0678], // [price, amount]
      [7963, 0.9162],
      [7961, 0.1],
      [7960, 12.8898],
      [7958, 1.2],
      [7955, 2.1009],
      [7954, 0.4708],
      [7953, 0.0564],
      [7951, 2.8031],
      [7950, 13.7785],
      [7949, 0.125],
      [7948, 4],
      [7942, 0.4337],
      [7940, 6.1612],
      [7936, 0.02],
      [7935, 1.3575],
      [7933, 2.002],
      [7932, 1.3449],
      [7930, 10.2974],
      [7929, 3.2226]
    ],
    "asks": [
      [7979, 0.0736],
      [7980, 1.0292],
      [7981, 5.5652],
      [7986, 0.2416],
      [7990, 1.9970],
      [7995, 0.88],
             */
            ExchangeOrderBook orders = new ExchangeOrderBook();
            JToken obj = await MakeJsonRequestAsync<JToken>("/market/depth?symbol=" + marketSymbol + "&type=step0",
                BaseUrl, null);
            return obj["tick"].ParseOrderBookFromJTokenArrays(sequence: "ts",
                maxCount: maxCount);
        }

     


        #region Private APIs

        private async Task<Dictionary<string, string>> OnGetAccountsAsync()
        {
            /*
            {[
  {
    "id": 3274515,
    "type": "spot",
    "subtype": "",
    "state": "working"
  },
  {
    "id": 4267855,
    "type": "margin",
    "subtype": "btcusdt",
    "state": "working"
  },
  {
    "id": 3544747,
    "type": "margin",
    "subtype": "ethusdt",
    "state": "working"
  },
  {
    "id": 3274640,
    "type": "otc",
    "subtype": "",
    "state": "working"
  }
]}
 */
            Dictionary<string, string> accounts = new Dictionary<string, string>();
            var payload = await GetNoncePayloadAsync();
            JToken data = await MakeJsonRequestAsync<JToken>("/account/accounts", BaseUrlV1, payload);
            foreach (var acc in data)
            {
                string key = acc["type"].ToStringInvariant() + "_" + acc["subtype"].ToStringInvariant();
                accounts.Add(key, acc["id"].ToStringInvariant());
            }

            return accounts;
        }


        protected override async Task<Dictionary<string, decimal>> OnGetAmountsAsync()
        {
            /*
             
  "status": "ok",
  "data": {
    "id": 3274515,
    "type": "spot",
    "state": "working",
    "list": [
      {
        "currency": "usdt",
        "type": "trade",
        "balance": "0.000045000000000000"
      },
      {
        "currency": "eth",
        "type": "frozen",
        "balance": "0.000000000000000000"
      },
      {
        "currency": "eth",
        "type": "trade",
        "balance": "0.044362165000000000"
      },
      {
        "currency": "eos",
        "type": "trade",
        "balance": "16.467000000000000000"
      },
             */
            var account_id = await GetAccountID();
            Dictionary<string, decimal> amounts = new Dictionary<string, decimal>();
            var payload = await GetNoncePayloadAsync();
            JToken token =
                await MakeJsonRequestAsync<JToken>($"/account/accounts/{account_id}/balance", BaseUrlV1, payload);
            var list = token["list"];
            foreach (var item in list)
            {
                var balance = item["balance"].ConvertInvariant<decimal>();
                if (balance == 0m)
                    continue;

                var currency = item["currency"].ToStringInvariant();

                if (amounts.ContainsKey(currency))
                {
                    amounts[currency] += balance;
                }
                else
                {
                    amounts[currency] = balance;
                }
            }

            return amounts;
        }

        protected override async Task<Dictionary<string, decimal>> OnGetAmountsAvailableToTradeAsync()
        {
            var account_id = await GetAccountID();

            Dictionary<string, decimal> amounts = new Dictionary<string, decimal>();
            var payload = await GetNoncePayloadAsync();
            JToken token =
                await MakeJsonRequestAsync<JToken>($"/account/accounts/{account_id}/balance", BaseUrlV1, payload);
            var list = token["list"];
            foreach (var item in list)
            {
                var balance = item["balance"].ConvertInvariant<decimal>();
                if (balance == 0m)
                    continue;
                var type = item["type"].ToStringInvariant();
                if (type != "trade")
                    continue;

                var currency = item["currency"].ToStringInvariant();

                if (amounts.ContainsKey(currency))
                {
                    amounts[currency] += balance;
                }
                else
                {
                    amounts[currency] = balance;
                }
            }

            return amounts;
        }

        protected override async Task<ExchangeOrderResult> OnGetOrderDetailsAsync(string orderId,
            string marketSymbol = null)
        {
            /*
             {{
              "status": "ok",
              "data": {
                "id": 3908501445,
                "symbol": "naseth",
                "account-id": 3274515,
                "amount": "0.050000000000000000",
                "price": "0.000001000000000000",
                "created-at": 1525100546601,
                "type": "buy-limit",
                "field-amount": "0.0",
                "field-cash-amount": "0.0",
                "field-fees": "0.0",
                "finished-at": 1525100816771,
                "source": "api",
                "state": "canceled",
                "canceled-at": 1525100816399
              }
            }}
             */
            var payload = await GetNoncePayloadAsync();
            JToken data = await MakeJsonRequestAsync<JToken>($"/order/orders/{orderId}", BaseUrlV1, payload);
            return ParseOrder(data);
        }

        protected override async Task<IEnumerable<ExchangeOrderResult>> OnGetCompletedOrderDetailsAsync(
            string marketSymbol = null, DateTime? afterDate = null)
        {
            if (marketSymbol == null)
            {
                throw new APIException("symbol cannot be null");
            }

            List<ExchangeOrderResult> orders = new List<ExchangeOrderResult>();
            var payload = await GetNoncePayloadAsync();
            payload.Add("symbol", marketSymbol);
            payload.Add("states", "partial-canceled,filled,canceled");
            if (afterDate != null)
            {
                payload.Add("start-date", afterDate.Value.ToString("yyyy-MM-dd"));
            }

            JToken data = await MakeJsonRequestAsync<JToken>("/order/orders", BaseUrlV1, payload);
            foreach (var prop in data)
            {
                orders.Add(ParseOrder(prop));
            }

            return orders;
        }

        protected override async Task<IEnumerable<ExchangeOrderResult>> OnGetOpenOrderDetailsAsync(
            string marketSymbol = null)
        {
            if (marketSymbol == null)
            {
                throw new APIException("symbol cannot be null");
            }

            List<ExchangeOrderResult> orders = new List<ExchangeOrderResult>();
            var payload = await GetNoncePayloadAsync();
            payload.Add("symbol", marketSymbol);
            payload.Add("states", "pre-submitted,submitting,submitted,partial-filled");
            JToken data = await MakeJsonRequestAsync<JToken>("/order/orders", BaseUrlV1, payload);
            foreach (var prop in data)
            {
                orders.Add(ParseOrder(prop));
            }

            return orders;
        }

        protected override async Task<ExchangeOrderResult> OnPlaceOrderAsync(ExchangeOrderRequest order)
        {
            var account_id = await GetAccountID(order.IsMargin, order.MarketSymbol);

            var payload = await GetNoncePayloadAsync();
            payload.Add("account-id", account_id);
            payload.Add("symbol", order.MarketSymbol);
            payload.Add("type", order.IsBuy ? "buy" : "sell");
            payload.Add("source", order.IsMargin ? "margin-api" : "api");

            decimal outputQuantity = ClampOrderQuantity(order.MarketSymbol, order.Amount);
            decimal outputPrice = ClampOrderPrice(order.MarketSymbol, order.Price);

            payload["amount"] = outputQuantity.ToStringInvariant();

            if (order.OrderType == OrderType.Market)
            {
                payload["type"] += "-market";
            }
            else
            {
                payload["type"] += "-limit";
                payload["price"] = outputPrice.ToStringInvariant();
            }

            order.ExtraParameters.CopyTo(payload);

            JToken obj = await MakeJsonRequestAsync<JToken>("/order/orders/place", BaseUrlV1, payload, "POST");
            order.Amount = outputQuantity;
            order.Price = outputPrice;
            return ParsePlaceOrder(obj, order);
        }

        protected override async Task OnCancelOrderAsync(string orderId, string marketSymbol = null)
        {
            var payload = await GetNoncePayloadAsync();
            await MakeJsonRequestAsync<JToken>($"/order/orders/{orderId}/submitcancel", BaseUrlV1, payload, "POST");
        }

        protected override Task<IEnumerable<ExchangeTransaction>> OnGetDepositHistoryAsync(string currency)
        {
            throw new NotImplementedException("Huobi does not provide a deposit API");
        }

        protected override Task<ExchangeDepositDetails> OnGetDepositAddressAsync(string currency,
            bool forceRegenerate = false)
        {
            throw new NotImplementedException("Huobi does not provide a deposit API");

            /*
            var payload = await GetNoncePayloadAsync();
            payload.Add("need_new", forceRegenerate ? 1 : 0);
            payload.Add("method", "GetDepositAddress");
            payload.Add("coinName", symbol);
            payload["method"] = "POST";
            // "return":{"address": 1UHAnAWvxDB9XXETsi7z483zRRBmcUZxb3,"processed_amount": 1.00000000,"server_time": 1437146228 }
            JToken token = await MakeJsonRequestAsync<JToken>("/", BaseUrlV1, payload, "POST");
            return new ExchangeDepositDetails
            {
                Address = token["address"].ToStringInvariant(),
                Symbol = symbol
            };
            */
        }

        protected override Task<ExchangeWithdrawalResponse> OnWithdrawAsync(ExchangeWithdrawalRequest withdrawalRequest)
        {
            throw new NotImplementedException("Huobi does not provide a withdraw API");
        }

        #endregion

        #region Private Functions

        protected override JToken CheckJsonResponse(JToken result)
        {
            if (result == null || (result["status"] != null && result["status"].ToStringInvariant() != "ok"))
            {
                throw new APIException((result["err-msg"] != null
                    ? result["err-msg"].ToStringInvariant()
                    : "Unknown Error"));
            }

            return result["data"] ?? result;
        }

        private ExchangeOrderResult ParsePlaceOrder(JToken token, ExchangeOrderRequest order)
        {
            /*
              {
                  "status": "ok",
                  "data": "59378"
                }
            */
            ExchangeOrderResult result = new ExchangeOrderResult
            {
                Amount = order.Amount,
                Price = order.Price,
                IsBuy = order.IsBuy,
                OrderId = token.ToStringInvariant(),
                MarketSymbol = order.MarketSymbol
            };
            result.AveragePrice = result.Price;
            result.Result = ExchangeAPIOrderResult.Pending;

            return result;
        }

        private ExchangeAPIOrderResult ParseState(string state)
        {
            switch (state)
            {
                case "pre-submitted":
                case "submitting":
                case "submitted":
                    return ExchangeAPIOrderResult.Pending;
                case "partial-filled":
                    return ExchangeAPIOrderResult.FilledPartially;
                case "filled":
                    return ExchangeAPIOrderResult.Filled;
                case "partial-canceled":
                case "canceled":
                    return ExchangeAPIOrderResult.Canceled;
                default:
                    return ExchangeAPIOrderResult.Unknown;
            }
        }

        private ExchangeOrderResult ParseOrder(JToken token)
        {
            ExchangeOrderResult result = new ExchangeOrderResult()
            {
                OrderId = token["id"].ToStringInvariant(),
                MarketSymbol = token["symbol"].ToStringInvariant(),
                Amount = token["amount"].ConvertInvariant<decimal>(),
                AmountFilled = token["field-amount"].ConvertInvariant<decimal>(),
                Price = token["price"].ConvertInvariant<decimal>(),
                OrderDate = CryptoUtility.UnixTimeStampToDateTimeMilliseconds(token["created-at"]
                    .ConvertInvariant<long>()),
                IsBuy = token["type"].ToStringInvariant().StartsWith("buy"),
                Result = ParseState(token["state"].ToStringInvariant()),
            };

            if (result.Price == 0 && result.AmountFilled != 0m)
            {
                var amountCash = token["field-cash-amount"].ConvertInvariant<decimal>();
                result.Price = amountCash / result.AmountFilled;
            }

            return result;
        }

        private List<ExchangeTrade> ParseTradesData(JToken token)
        {
            var trades = new List<ExchangeTrade>();
            foreach (var t in token)
            {
                trades.Add(t.ParseTrade("amount", "price", "direction", "ts", TimestampType.UnixMilliseconds, "id"));
            }

            return trades;
        }

        private async Task<string> GetAccountID(bool isMargin = false, string subtype = "")
        {
            var accounts = await OnGetAccountsAsync();
            var key = "spot_";
            if (isMargin)
            {
                key = "margin_" + subtype;
            }

            var account_id = accounts[key];
            return account_id;
        }

        #endregion
    }

}
