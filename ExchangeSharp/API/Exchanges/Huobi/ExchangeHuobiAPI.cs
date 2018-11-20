using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Centipede.API.Exchanges.Formatter;

namespace Centipede
{

    enum AccountTypeEnum
    {
        Spot = 0,
        Margin = 1,
        Otc = 2,
        Point = 3
    }


    [ExchangeMeta("Huobi")]
    public sealed class ExchangeHuobiAPI : ExchangeAPI
    {
        public override string BaseUrl { get; set; } = "https://api.huobipro.com";
        public string BaseUrlV1 { get; set; } = "https://api.huobipro.com/v1";
        public override string BaseUrlWebSocket { get; set; } = "wss://api.huobipro.com/ws";
        private long _webSocketId = 0;


        private Dictionary<AccountTypeEnum, string> Accounts { get; set; }

        public ExchangeHuobiAPI()
        {
            RequestContentType = "application/x-www-form-urlencoded";
            NonceStyle = NonceStyle.UnixMilliseconds;

            WebSocketDepthType = WebSocketDepthType.FullBookAlways;
            CurrentTimestampType = TimestampType.UnixMilliseconds;

            this.KeysLoaded += ExchangeHuobiAPI_KeysLoaded;
        }

        private async void ExchangeHuobiAPI_KeysLoaded()
        {
            await this.LoadAccountsAsync();
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
                   时间戳（DateTime） 您发出请求的时间 (UTC 时区) (UTC 时区) (UTC 时区) 。在查询请求中包含此值有助于防止第三方截取您的请求。如：2017-05-11T16:22:06。再次强调是 (UTC 时区) 。
                   必选和可选参数 每个方法都有一组用于定义 API 调用的必需参数和可选参数。可以在每个方法的说明中查看这些参数及其含义。 请一定注意：对于GET请求，每个方法自带的参数都需要进行签名运算； 对于POST请求，每个方法自带的参数不进行签名认证，即POST请求中需要进行签名运算的只有AccessKeyId、SignatureMethod、SignatureVersion、Timestamp四个参数，其它参数放在body中。
                   签名 签名计算得出的值，用于确保签名有效和未被篡改。
                 */


                // must sort case sensitive
                var dict = new SortedDictionary<string, object>(StringComparer.Ordinal)
                {
                    ["DateTime"] =
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

        #region websocket通用

        /// <summary>
        /// 响应websocket返回的msg信息
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="msg"></param>
        /// <param name="symbols"></param>
        /// <param name="dataKey"></param>
        /// <returns></returns>
        private async Task<Tuple<JToken, Symbol>> ProcessWebsocketMessage(IWebSocket socket, byte[] msg,
            Symbol[] symbols, string dataKey = "tick")
        {
            var str = msg.ToStringFromUTF8Gzip();
            JToken token = JToken.Parse(str);

            if (token["status"] != null)
            {
                //todo:订阅失败
                return null;
            }

            if (token["ping"] != null)
            {
                await socket.SendMessageAsync(str.Replace("ping", "pong"));
                return null;
            }

            var val = token["ch"].ToStringInvariant();

            if (symbols == null)
                symbols = this.Symbols.ToArray();

            var symbol = GetSysmbol(val, symbols);

            if (symbol == null) //订阅错误
                return null;

            JToken data = token[dataKey];

            return new Tuple<JToken, Symbol>(data, symbol);
        }

        private Symbol GetSysmbol(string ch, Symbol[] symbols)
        {
            var sArray = ch.Split('.');
            var originSymbol = sArray[1].ToStringInvariant();
            var symbol = symbols.FirstOrDefault(p => p.OriginSymbol == originSymbol);
            return symbol;
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

        private TickerFormatter GetTickerFormatter(FormatterTypeEnum type)
        {
            /* ALL
             *  {"open":0.008545,"close":0.008656,"low":0.008088,"high":0.009388,"amount":88056.1860,
             *  "count":16077,"vol":771.7975953754,"symbol":"ltcbtc"}
             */

            /*single
             *{"id":1499225271,"ts":1499225271000,"close":1885.0000,"open":1960.0000,"high":1985.0000,"low":1856.0000,
             * "amount":81486.2926,"count":42122,"vol":157052744.85708200,"ask":[1885.0000,21.8804],"bid":[1884.0000,1.6702]}
             */

            /*websocket
             *"amount":12224.2922,"open":9790.52,"close":10195.00,"high":10300.00,"ts":1494496390000,
             * "id":1494496390,"count":15195,"low":9657.00,"vol":121906001.754751
             */

            var result = new TickerFormatter
            {
                LastKey = "close",
                VolumeFormatter = new VolumeFormatter
                {
                    BaseVolumeKey = "amount",
                    QuoteVolumeKey = "vol"
                }
            };

            if (type != FormatterTypeEnum.All)
            {
                result.TimestampFormatter = new TimestampFormatter
                {
                    TimestampKey = "ts",
                    TimestampType = CurrentTimestampType
                };

                result.IdKey = "id";

                if (type == FormatterTypeEnum.Signle)
                {
                    result.AskBidFormatter = new AskBidFormatter
                    {
                        AskKey = "ask",
                        BidKey = "bid"
                    };
                }
            }

            return result;
        }


        public override async Task<ExchangeTicker> GetTickerAsync(Symbol symbol)
        {
            var formatter = this.GetTickerFormatter(FormatterTypeEnum.Signle);

            JToken ticker = await MakeJsonRequestAsync<JToken>("/market/detail/merged?symbol=" + symbol.OriginSymbol);
            var data = ticker["tick"];
            return data.ParseTicker(symbol, formatter);
        }

        public override async Task<List<ExchangeTicker>> GetTickersAsync()
        {

            List<ExchangeTicker> tickers = new List<ExchangeTicker>();
            JToken obj = await MakeJsonRequestAsync<JToken>("/market/tickers", BaseUrl);
            var formatter = this.GetTickerFormatter(FormatterTypeEnum.All);

            foreach (JToken child in obj)
            {
                var symbol = this.Symbols.Get(child["symbol"].ToStringInvariant());
                tickers.Add(child.ParseTicker(symbol, formatter));
            }

            return tickers;
        }


        public override IWebSocket GetTickerWebSocket(Action<ExchangeTicker> callback, params Symbol[] symbols)
        {
            var formatter = this.GetTickerFormatter(FormatterTypeEnum.Websocket);

            return ConnectWebSocket(string.Empty, async (socket, msg) =>
            {
                var data = await ProcessWebsocketMessage(socket, msg, symbols);
                var ticker = data.Item1.ParseTicker(data.Item2, formatter);

                callback(ticker);
            }, async (socket) =>
            {
                foreach (var symbol in symbols)
                {
                    var id = System.Threading.Interlocked.Increment(ref _webSocketId);
                    var channel = $"market.{symbol.OriginSymbol}.detail";
                    await socket.SendMessageAsync(new {sub = channel, id = "id" + id.ToStringInvariant()});
                    /* {"sub":"market.$symbol.detail","id":"id generated by client"}*/
                }
            });
        }

        #endregion

        #region  trades


        public override async Task<List<ExchangeTrade>> GetTradesAsync(Symbol symbol, int limit = 20)
        {


            var formatter = GetTradeFormatter(FormatterTypeEnum.Signle);

            JToken result =
                await MakeJsonRequestAsync<JToken>($"/market/history/trade?symbol={symbol.OriginSymbol}&size={limit}",
                    BaseUrl);

            if (result is JArray data)
            {
                var trades =
                    data.Select(p => p.ParseTrade(symbol, formatter))
                        .ToList();
                return trades;
            }

            return null;
        }

        public override IWebSocket GetTradesWebSocket(Action<List<ExchangeTrade>> callback,
            params Symbol[] symbols)
        {
            var formatter = GetTradeFormatter(FormatterTypeEnum.Websocket);

            return ConnectWebSocket(string.Empty, async (socket, msg) =>
            {
                var result = await ProcessWebsocketMessage(socket, msg, symbols);

                if (result.Item1["data"] is JArray data)
                {
                    var trades =
                        data.Select(p => p.ParseTrade(result.Item2, formatter))
                            .ToList();
                    callback(trades);
                }

            }, async (socket) =>
            {
                foreach (Symbol symbol in symbols)
                {
                    long id = System.Threading.Interlocked.Increment(ref _webSocketId);
                    string channel = $"market.{symbol.OriginSymbol}.trade.detail";
                    await socket.SendMessageAsync(new {sub = channel, id = "id" + id.ToStringInvariant()});
                }
            });
        }

        private TradeFormatter GetTradeFormatter(FormatterTypeEnum type)
        {
            return new TradeFormatter
            {
                AmountKey = "amount",
                PriceKey = "price",
                DirectionKey = "direction",
                TimestampFormatter = new TimestampFormatter
                {
                    TimestampKey = "ts",
                    TimestampType = CurrentTimestampType
                },
                IdKey = "id",
                DirectionIsBuyValue = "buy"
            };

            /* websocket
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

            /*
            * {"status":"ok","ch":"market.ethusdt.trade.detail","ts":1542104543483,
            * "tick":{"id":27952707649,"ts":1542104541926,
            *  "data":[{"amount":0.100000000000000000,"ts":1542104541926,"id":2795270764916609236638,"price":210.900000000000000000,"direction":"buy"},{"amount":0.327700000000000000,"ts":1542104541926,"id":2795270764916609248055,"price":210.900000000000000000,"direction":"buy"}]}}
            */
        }

        #endregion

        #region  kline

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


            var formatter = GetCandleFormatter(FormatterTypeEnum.Signle);

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
                candles.Add(token.ParseCandle(symbol, periodSeconds, formatter));
            }

            //注意先插下k线的顺序
            candles.Reverse();
            return candles;
        }

        public override IWebSocket GetCandlesWebSocket(Action<MarketCandle> callback, int periodSeconds,
            Symbol[] symbols)
        {
            var formatter = GetCandleFormatter(FormatterTypeEnum.Websocket);

            return ConnectWebSocket(string.Empty, async (socket, msg) =>
            {

                var data = await ProcessWebsocketMessage(socket, msg, symbols);

                var candles = data.Item1.ParseCandle(data.Item2, periodSeconds, formatter);

                callback(candles); //todo:检查下顺序

            }, async (socket) =>
            {
                if (symbols == null || symbols.Length == 0)
                {
                    symbols = Symbols.ToArray();
                }

                foreach (var symbol in symbols)
                {
                    //"sub": "market.btcusdt.kline.1min",
                    var id = System.Threading.Interlocked.Increment(ref _webSocketId);
                    var channel =
                        $"market.{symbol.OriginSymbol}.depth.{CryptoUtility.SecondsToPeriodStringLong(periodSeconds)}";
                    await socket.SendMessageAsync(new {sub = channel, id = "id" + id.ToStringInvariant()});
                }
            });
        }



        private CandleFormatter GetCandleFormatter(FormatterTypeEnum type)
        {

            /*websocket
             "ch":"market.btcusdt.kline.1min","ts":1489474082831,
             "tick":{"id":1489464480,"amount":0.0,"count":0,"open":7962.62,"close":7962.62,"low":7962.62,"high":7962.62,"vol":0.0}
             */

            /* single
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
            return new CandleFormatter
            {
                OpenKey = "open",
                HighKey = "high",
                LowKey = "low",
                CloseKey = "close",
                TimestampFormatter = new TimestampFormatter
                {
                    TimestampType = TimestampType.UnixSeconds, // K线的时间戳是到秒的
                    TimestampKey = "id",
                },
                VolumeFormatter = new VolumeFormatter
                {
                    BaseVolumeKey = null,
                    QuoteVolumeKey = "vol"
                }
            };
        }

        #endregion

        #region depth

        public override async Task<ExchangeDepth> GetDepthAsync(Symbol symbol, int maxCount)
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
      [7958, 1.2]
    ],
    "asks": [
      [7979, 0.0736],
      [7980, 1.0292],
      [7981, 5.5652],
      [7986, 0.2416],
      [7990, 1.9970],
      [7995, 0.88],
             */
            JToken obj = await MakeJsonRequestAsync<JToken>(
                "/market/depth?symbol=" + symbol.OriginSymbol + "&type=step0", BaseUrl);

            return obj["tick"].ParseDepthFromJTokenArrays(symbol, maxCount: maxCount);
        }

        public override IWebSocket GetDepthWebSocket(Action<ExchangeDepth> callback, int maxCount = 20,
            params Symbol[] symbols)
        {
            return ConnectWebSocket(string.Empty, async (socket, msg) =>
            {
                /* {"ch":"market.btcusdt.depth.step0","ts":1526749254037,
                    "tick":{
                    "bids":[[8268.3,0.101],[8268.29,0.8248]],
                    "asks":[[8275.07,0.1961],[8337.1,0.5803]],
                    "ts":1526749254016,"version":7664175145}}
                 */
                var result = await ProcessWebsocketMessage(socket, msg, symbols);
                var depth = result.Item1.ParseDepthFromJTokenArrays(result.Item2, maxCount: maxCount);
                callback(depth);

            }, async (socket) =>
            {
                foreach (var symbol in symbols)
                {
                    var id = System.Threading.Interlocked.Increment(ref _webSocketId);
                    var channel = $"market.{symbol.OriginSymbol}.depth.step0";
                    await socket.SendMessageAsync(new {sub = channel, id = "id" + id.ToStringInvariant()});
                    /*
                     * {"id":"id1","status":"ok","subbed":"market.btcusdt.depth.step0","ts":1526749164133}
                     */
                }
            });
        }

        #endregion

        #region order

        /// <summary>
        /// 提交订单
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public override async Task<List<ExchangeOrderResult>> PlaceOrdersAsync(params ExchangeOrderRequest[] orders)
        {
            var accountId = Accounts[AccountTypeEnum.Spot];

            var result = new List<ExchangeOrderResult>();

            foreach (var order in orders)
            {

                var payload = await GetNoncePayloadAsync();
                payload.Add("account-id", accountId);
                payload.Add("symbol", order.Symbol.OriginSymbol);
                payload.Add("type", order.IsBuy ? "buy" : "sell");
                payload.Add("source", order.IsMargin ? "margin-api" : "api");

                if (order.OrderType == OrderType.Market)
                {
                    //市价买单时表示买多少钱，市价卖单时表示卖多少币
                    payload["type"] += "-market";
                    //这里到时候在设计下市价的情况下怎么做这个，应该一般比较少做
                    payload["amount"] = order.Amount;
                }
                else
                {
                    //限价单表示下单数量
                    decimal outputQuantity = ClampOrderQuantity(order.Symbol, order.Amount);
                    decimal outputPrice = ClampOrderPrice(order.Symbol, order.Price);

                    order.Amount = outputQuantity;
                    order.Price = outputPrice;

                    payload["amount"] = outputQuantity.ToStringInvariant();
                    payload["type"] += "-limit";
                    payload["price"] = outputPrice.ToStringInvariant();
                }

                order.ExtraParameters.CopyTo(payload);

                JToken data = await MakeJsonRequestAsync<JToken>("/order/orders/place", BaseUrlV1, payload, "POST");

                result.Add(ParsePlaceOrder(data, order));
            }

            return result;
        }

        /// <summary>
        /// 取消订单
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public override async Task CancelOrdersAsync(params ExchangeOrderCancelRequest[] orders)
        {
            var payload = await GetNoncePayloadAsync();
            var oids = string.Join(",", orders.SelectMany(p => p.OrderIds));
            payload.Add("order-ids", oids);
            await MakeJsonRequestAsync<JToken>("/v1/order/orders/batchcancel", BaseUrlV1, payload, "POST");
        }


        public override async Task<IEnumerable<ExchangeOrderResult>> GetCompletedOrderDetailsAsync(
            Symbol symbol = null, DateTime? afterDate = null)
        {
            if (symbol == null)
            {
                throw new APIException("symbol cannot be null");
            }

            List<ExchangeOrderResult> orders = new List<ExchangeOrderResult>();
            var payload = await GetNoncePayloadAsync();
            payload.Add("symbol", symbol);
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

        public override async Task<IEnumerable<ExchangeOrderResult>> GetOpenOrderDetailsAsync(Symbol symbol = null)
        {
            List<ExchangeOrderResult> orders = new List<ExchangeOrderResult>();
            var payload = await GetNoncePayloadAsync();

            if (symbol != null)
            {
                payload.Add("symbol", symbol.OriginSymbol);
                payload.Add("account-id", Accounts[AccountTypeEnum.Spot]);
            }

            payload.Add("size", "500");

            JToken data = await MakeJsonRequestAsync<JToken>("/v1/order/openOrders", BaseUrlV1, payload);

            foreach (var prop in data)
            {
                orders.Add(ParseOrder(prop));
            }

            return orders;
        }


        #endregion

        #region account
        public override async Task<List<ExchangeFinance>> GetFinanceAsync()
        {
            /*"data": {"list": [
                  {
                    "currency": "usdt",
                    "type": "trade",
                    "balance": "0.000045000000000000"
                  },
                  {
                    "currency": "eth",
                    "type": "frozen",
                    "balance": "0.000000000000000000"
                  }, */

            var accountId = Accounts[AccountTypeEnum.Spot];

            var payload = await GetNoncePayloadAsync();
            JToken token =
                await MakeJsonRequestAsync<JToken>($"/account/accounts/{accountId}/balance", BaseUrlV1, payload);

            var list = token["list"];
            var result = new List<ExchangeFinance>();

            foreach (var item in list)
            {
                var currency =
                    this.Currencies.FirstOrDefault(p => p.OriginCurrency == item["currency"].ToStringInvariant());

                var finance = result.FirstOrDefault(p => p.Currency == currency);

                if (finance == null)
                {
                    finance = new ExchangeFinance
                    {
                        Currency = currency
                    };

                    result.Add(finance);
                }


                if (item["type"].ToStringInvariant() == "frozen")
                {
                    finance.Hold = item["balance"].ConvertInvariant<decimal>();
                }
                else
                {
                    finance.Available = item["balance"].ConvertInvariant<decimal>();
                }

                finance.Balance = finance.Available + finance.Hold;
            }

            return result;
        }

        private async Task LoadAccountsAsync()
        {
            Accounts.Clear();

            var payload = await GetNoncePayloadAsync();
            JToken data = await MakeJsonRequestAsync<JToken>("/account/accounts", BaseUrlV1, payload);
            foreach (var acc in data)
            {
                string key = acc["type"].ToStringInvariant();
                AccountTypeEnum type;

                switch (key)
                {
                    case "spot":
                        type = AccountTypeEnum.Spot;
                        break;
                    case "margin":
                        type = AccountTypeEnum.Margin;
                        break;

                    case "otc":
                        type = AccountTypeEnum.Otc;
                        break;

                    case "point":
                        type = AccountTypeEnum.Point;
                        break;
                    default:
                        type = AccountTypeEnum.Spot;
                        break;
                }

                Accounts.Add(type, acc["id"].ToStringInvariant());
            }
        }

        #endregion
        #region Private Functions

        protected override JToken CheckJsonResponse(JToken result)
        {
            if (result == null || (result["status"] != null && result["status"].ToStringInvariant() != "ok"))
            {
                throw new APIException((result?["err-msg"] != null
                    ? result["err-msg"].ToStringInvariant()
                    : "Unknown Error"));
            }

            return result["data"] ?? result;
        }

        /// <summary>
        /// 下单对象转换为订单结果对象
        /// </summary>
        /// <param name="token"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        private ExchangeOrderResult ParsePlaceOrder(JToken token, ExchangeOrderRequest order)
        {
            ExchangeOrderResult result = new ExchangeOrderResult
            {
                Amount = order.Amount,
                Price = order.Price,
                IsBuy = order.IsBuy,
                OrderId = token.ToStringInvariant(),
                Symbol = order.Symbol,
                Result = ExchangeAPIOrderResult.Pending
            };

            return result;
        }

        /// <summary>
        /// 转换状态
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
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
                Symbol = this.Symbols.Get(token["symbol"].ToStringInvariant()),
                Amount = token["amount"].ConvertInvariant<decimal>(),
                Price = token["price"].ConvertInvariant<decimal>(),

                OrderDate = token.ParseDatetime(new TimestampFormatter { TimestampType = CurrentTimestampType , TimestampKey = "created-at" }),

                AmountFilled = token["filled-amount"].ConvertInvariant<decimal>(),
                Fees = token["filled-fees"].ConvertInvariant<decimal>(),

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

        #endregion
    }
}

