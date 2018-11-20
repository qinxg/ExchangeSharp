using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Centipede
{
    /// <summary>Contains useful extension methods and parsing for the ExchangeAPI classes</summary>
    public static class ExchangeAPIExtensions
    {
        /// <summary>
        ///     Place a limit order by first querying the order book and then placing the order for a threshold below the bid or
        ///     above the ask that would fully fulfill the amount.
        ///     The order book is scanned until an amount of bids or asks that will fulfill the order is found and then the order
        ///     is placed at the lowest bid or highest ask price multiplied
        ///     by priceThreshold.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="symbol">Symbol to sell</param>
        /// <param name="amount">Amount to sell</param>
        /// <param name="isBuy">True for buy, false for sell</param>
        /// <param name="orderBookCount">Amount of bids/asks to request in the order book</param>
        /// <param name="priceThreshold">
        ///     Threshold below the lowest bid or above the highest ask to set the limit order price at. For buys, this is
        ///     converted to 1 / priceThreshold.
        ///     This can be set to 0 if you want to set the price like a market order.
        /// </param>
        /// <param name="thresholdToAbort">
        ///     If the lowest bid/highest ask price divided by the highest bid/lowest ask price is below this threshold, throw an
        ///     exception.
        ///     This ensures that your order does not buy or sell at an extreme margin.
        /// </param>
        /// <param name="abortIfOrderBookTooSmall">
        ///     Whether to abort if the order book does not have enough bids or ask amounts to
        ///     fulfill the order.
        /// </param>
        /// <returns>Order result</returns>
        public static async Task<List<ExchangeOrderResult>> PlaceSafeMarketOrderAsync(this ExchangeAPI api, string symbol,
            decimal amount, bool isBuy, int orderBookCount = 100, decimal priceThreshold = 0.9m,
            decimal thresholdToAbort = 0.75m, bool abortIfOrderBookTooSmall = false)
        {
            if (priceThreshold > 0.9m)
                throw new APIException(
                    "You cannot specify a price threshold above 0.9m, otherwise there is a chance your order will never be fulfilled. For buys, this is " +
                    "converted to 1.0m / priceThreshold, so always specify the value below 0.9m");
            if (priceThreshold <= 0m)
                priceThreshold = 1m;
            else if (isBuy && priceThreshold > 0m) priceThreshold = 1.0m / priceThreshold;

            ExchangeDepth book = null; //todo await api.GetDepthAsync(symbol, orderBookCount);
            if (book == null || isBuy && book.Asks.Count == 0 || !isBuy && book.Bids.Count == 0)
                throw new APIException($"Error getting order book for {symbol}");

            var counter = 0m;
            var highPrice = decimal.MinValue;
            var lowPrice = decimal.MaxValue;
            if (isBuy)
                foreach (var ask in book.Asks.Values)
                {
                    counter += ask.Amount;
                    highPrice = Math.Max(highPrice, ask.Price);
                    lowPrice = Math.Min(lowPrice, ask.Price);
                    if (counter >= amount) break;
                }
            else
                foreach (var bid in book.Bids.Values)
                {
                    counter += bid.Amount;
                    highPrice = Math.Max(highPrice, bid.Price);
                    lowPrice = Math.Min(lowPrice, bid.Price);
                    if (counter >= amount) break;
                }

            if (abortIfOrderBookTooSmall && counter < amount)
                throw new APIException(
                    $"{(isBuy ? "Buy" : "Sell")} order for {symbol} and amount {amount} cannot be fulfilled because the order book is too thin.");
            if (lowPrice / highPrice < thresholdToAbort)
                throw new APIException(
                    $"{(isBuy ? "Buy" : "Sell")} order for {symbol} and amount {amount} would place for a price below threshold of {thresholdToAbort}, aborting.");

            var request = new ExchangeOrderRequest
            {
                Amount = amount,
                OrderType = OrderType.Limit,
                Price = CryptoUtility.RoundAmount((isBuy ? highPrice : lowPrice) * priceThreshold),
                ShouldRoundAmount = true,
                Symbol = null //todo symbol
            };
            var result = await api.PlaceOrdersAsync(request);

            // wait about 10 seconds until the order is fulfilled
            var i = 0;
            const int maxTries = 20; // 500 ms for each try
            for (; i < maxTries; i++)
                await Task.Delay(500);
            //TODO
            //result = await api.GetCanceledOrdersAsync(result.OrderId, symbol);
            //switch (result.Result)
            //{
            //    case ExchangeAPIOrderResult.Filled:
            //    case ExchangeAPIOrderResult.Canceled:
            //    case ExchangeAPIOrderResult.Error:
            //        break;
            //}

            if (i == maxTries)
                throw new APIException(
                    $"{(isBuy ? "Buy" : "Sell")} order for {symbol} and amount {amount} timed out and may not have been fulfilled");

            return result;
        }

        #region market

        #region depth

        /// <summary>
        ///     Common order book parsing method, most exchanges use "asks" and "bids" with
        ///     arrays of length 2 for price and amount (or amount and price)
        /// </summary>
        /// <param name="token">Token</param>
        /// <param name="symbol"></param>
        /// <param name="asks">Asks key</param>
        /// <param name="bids">Bids key</param>
        /// <param name="timestampType"></param>
        /// <param name="maxCount">Max count</param>
        /// <param name="sequence"></param>
        /// <returns>Order book</returns>
        internal static ExchangeDepth ParseDepthFromJTokenArrays
        (
            this JToken token,
            Symbol symbol,
            string asks = "asks",
            string bids = "bids",
            string sequence = "ts",
            TimestampType timestampType = TimestampType.None,
            int maxCount = 100
        )
        {
            var book = new ExchangeDepth
            {
                SequenceId = token[sequence].ConvertInvariant<long>(),
                LastUpdatedUtc = CryptoUtility.ParseTimestamp(token[sequence], timestampType),
                Symbol = symbol
            };

            foreach (JArray array in token[asks])
            {
                var depth = new ExchangeOrderPrice
                {
                    Price = array[0].ConvertInvariant<decimal>(),
                    Amount = array[1].ConvertInvariant<decimal>()
                };
                book.Asks[depth.Price] = depth;
                if (book.Asks.Count == maxCount) break;
            }

            foreach (JArray array in token[bids])
            {
                var depth = new ExchangeOrderPrice
                {
                    Price = array[0].ConvertInvariant<decimal>(),
                    Amount = array[1].ConvertInvariant<decimal>()
                };
                book.Bids[depth.Price] = depth;
                if (book.Bids.Count == maxCount) break;
            }

            return book;
        }

        /// <summary>
        ///     Common order book parsing method, checks for "amount" or "quantity" and "price"
        ///     elements
        /// </summary>
        /// <param name="token">Token</param>
        /// <param name="asks">Asks key</param>
        /// <param name="bids">Bids key</param>
        /// <param name="price">Price key</param>
        /// <param name="amount">Quantity key</param>
        /// <param name="sequence">Sequence key</param>
        /// <param name="maxCount">Max count</param>
        /// <returns>Order book</returns>
        internal static ExchangeDepth ParseDepthFromJTokenDictionaries
        (
            this JToken token,
            string asks = "asks",
            string bids = "bids",
            string price = "price",
            string amount = "amount",
            string sequence = "ts",
            int maxCount = 100
        )
        {
            var book = new ExchangeDepth {SequenceId = token[sequence].ConvertInvariant<long>()};
            foreach (var ask in token[asks])
            {
                var depth = new ExchangeOrderPrice
                {
                    Price = ask[price].ConvertInvariant<decimal>(),
                    Amount = ask[amount].ConvertInvariant<decimal>()
                };
                book.Asks[depth.Price] = depth;
                if (book.Asks.Count == maxCount) break;
            }

            foreach (var bid in token[bids])
            {
                var depth = new ExchangeOrderPrice
                {
                    Price = bid[price].ConvertInvariant<decimal>(),
                    Amount = bid[amount].ConvertInvariant<decimal>()
                };
                book.Bids[depth.Price] = depth;
                if (book.Bids.Count == maxCount) break;
            }

            return book;
        }

        #endregion



       

        /// <summary>
        ///     Parse a JToken into a ticker
        /// </summary>
        /// <param name="token">Token</param>
        /// <param name="symbol"></param>
        /// <param name="formatter"></param>
        /// <returns>ExchangeTicker</returns>
        internal static ExchangeTicker ParseTicker(this JToken token, Symbol symbol, TickerFormatter formatter)
        {
            if (token == null || !token.HasValues) return null;

            var last = token[formatter.LastKey].ConvertInvariant<decimal>();

            token.ParseVolumes(formatter.VolumeFormatter,
                last,
                out var baseCurrencyVolume,
                out var quoteCurrencyVolume);

            DateTime date = token.ParseDatetime(formatter.TimestampFormatter);

            decimal? ask = null;
            decimal? bid = null;

            token.ParseAskBid(formatter.AskBidFormatter, ref ask, ref bid);

            var ticker = new ExchangeTicker
            {
                Symbol = symbol,
                Ask = ask,
                Bid = bid,
                Id = token[formatter.IdKey] == null ? null : token[formatter.IdKey].ToStringInvariant(),
                Last = last,
                BaseCurrencyVolume = baseCurrencyVolume,
                QuoteCurrencyVolume = quoteCurrencyVolume,
                DateTime = date
            };

            return ticker;
        }


        public static void ParseAskBid(this JToken token, AskBidFormatter formatter, ref decimal? ask, ref decimal? bid)
        {

            if (formatter?.AskKey != null)
            {
                var askValue = token[formatter.AskKey];
                ask = askValue is JArray
                    ? askValue[0].ConvertInvariant<decimal>()
                    : askValue.ConvertInvariant<decimal>();
            }

            if (formatter?.BidKey != null)
            {
                var bidValue = token[formatter.BidKey];
                bid = bidValue is JArray
                    ? bidValue[0].ConvertInvariant<decimal>()
                    : bidValue.ConvertInvariant<decimal>();
            }
        }

        public static DateTime ParseDatetime(this JToken token, TimestampFormatter formatter)
        {
            DateTime date = formatter?.TimestampKey == null
                ? CryptoUtility.UtcNow
                : CryptoUtility.ParseTimestamp(token[formatter.TimestampKey], formatter.TimestampType);

            return date;
        }


        /// <summary>
        ///     Parse a trade
        /// </summary>
        /// <param name="token">Token</param>
        /// <param name="symbol"></param>
        /// <param name="formatter"></param>
        /// <returns>Trade</returns>
        internal static ExchangeTrade ParseTrade(this JToken token, Symbol symbol, TradeFormatter formatter)
        {

            DateTime date = token.ParseDatetime(formatter.TimestampFormatter);

            var trade = new ExchangeTrade
            {
                Symbol = symbol,
                Amount = token[formatter.AmountKey].ConvertInvariant<decimal>(),
                Price = token[formatter.PriceKey].ConvertInvariant<decimal>(),
                IsBuy = token[formatter.DirectionKey].ToStringInvariant().EqualsWithOption(formatter.DirectionIsBuyValue),
                DateTime = date
            };

            trade.Id = formatter.IdKey == null
                ? trade.DateTime.Ticks.ToString()
                : token[formatter.IdKey].ToStringInvariant();

            return trade;
        }

        /// <summary>
        ///     Parse volume from JToken
        /// </summary>
        /// <param name="token">JToken</param>
        /// <param name="formatter"></param>
        /// <param name="last">Last volume value</param>
        /// <param name="baseCurrencyVolume">Receive base currency volume</param>
        /// <param name="quoteCurrencyVolume">Receive quote currency volume</param>
        internal static void ParseVolumes(this JToken token,
            VolumeFormatter formatter, decimal last, out decimal baseCurrencyVolume, out decimal quoteCurrencyVolume)
        {
            // parse out volumes, handle cases where one or both do not exist
            if (formatter.BaseVolumeKey == null)
            {
                if (formatter.QuoteVolumeKey == null)
                {
                    baseCurrencyVolume = quoteCurrencyVolume = 0m;
                }
                else
                {
                    quoteCurrencyVolume = token[formatter.QuoteVolumeKey].ConvertInvariant<decimal>();
                    baseCurrencyVolume = last <= 0m ? 0m : quoteCurrencyVolume / last;
                }
            }
            else
            {
                baseCurrencyVolume = token[formatter.BaseVolumeKey].ConvertInvariant<decimal>();

                if (formatter.QuoteVolumeKey == null)
                    quoteCurrencyVolume = baseCurrencyVolume * last;
                else
                    quoteCurrencyVolume = token[formatter.QuoteVolumeKey].ConvertInvariant<decimal>();
            }
        }

        /// <summary>
        ///     Parse market candle from JArray
        /// </summary>
        /// <param name="token">JToken</param>
        /// <param name="symbol"></param>
        /// <param name="periodSeconds"></param>
        /// <param name="formatter"></param>
        /// <returns>MarketCandle</returns>
        internal static MarketCandle ParseCandle(this JToken token, Symbol symbol, int periodSeconds, CandleFormatter formatter)
        {
            DateTime date = token.ParseDatetime(formatter.TimestampFormatter);

            var candle = new MarketCandle
            {
                Symbol = symbol,
                ClosePrice = token[formatter.CloseKey].ConvertInvariant<decimal>(),
                HighPrice = token[formatter.HighKey].ConvertInvariant<decimal>(),
                LowPrice = token[formatter.LowKey].ConvertInvariant<decimal>(),
                OpenPrice = token[formatter.OpenKey].ConvertInvariant<decimal>(),
                PeriodSeconds = periodSeconds,
                DateTime = date
            };

            token.ParseVolumes(formatter.VolumeFormatter, candle.ClosePrice,
                out var baseVolume, out var convertVolume);

            candle.BaseCurrencyVolume = (double) baseVolume;
            candle.QuoteCurrencyVolume = (double) convertVolume;

            if (formatter.WeightedAverageKey != null)
                candle.WeightedAverage = token[formatter.WeightedAverageKey].ConvertInvariant<decimal>();

            return candle;
        }

        #endregion
    }
}