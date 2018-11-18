using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>Contains useful extension methods and parsing for the ExchangeAPI classes</summary>
    public static class ExchangeAPIExtensions
    {

        /// <summary>
        /// Place a limit order by first querying the order book and then placing the order for a threshold below the bid or above the ask that would fully fulfill the amount.
        /// The order book is scanned until an amount of bids or asks that will fulfill the order is found and then the order is placed at the lowest bid or highest ask price multiplied
        /// by priceThreshold.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="symbol">Symbol to sell</param>
        /// <param name="amount">Amount to sell</param>
        /// <param name="isBuy">True for buy, false for sell</param>
        /// <param name="orderBookCount">Amount of bids/asks to request in the order book</param>
        /// <param name="priceThreshold">Threshold below the lowest bid or above the highest ask to set the limit order price at. For buys, this is converted to 1 / priceThreshold.
        /// This can be set to 0 if you want to set the price like a market order.</param>
        /// <param name="thresholdToAbort">If the lowest bid/highest ask price divided by the highest bid/lowest ask price is below this threshold, throw an exception.
        /// This ensures that your order does not buy or sell at an extreme margin.</param>
        /// <param name="abortIfOrderBookTooSmall">Whether to abort if the order book does not have enough bids or ask amounts to fulfill the order.</param>
        /// <returns>Order result</returns>
        public static async Task<ExchangeOrderResult> PlaceSafeMarketOrderAsync(this ExchangeAPI api, string symbol,
            decimal amount, bool isBuy, int orderBookCount = 100, decimal priceThreshold = 0.9m,
            decimal thresholdToAbort = 0.75m, bool abortIfOrderBookTooSmall = false)
        {
            if (priceThreshold > 0.9m)
            {
                throw new APIException(
                    "You cannot specify a price threshold above 0.9m, otherwise there is a chance your order will never be fulfilled. For buys, this is " +
                    "converted to 1.0m / priceThreshold, so always specify the value below 0.9m");
            }
            else if (priceThreshold <= 0m)
            {
                priceThreshold = 1m;
            }
            else if (isBuy && priceThreshold > 0m)
            {
                priceThreshold = 1.0m / priceThreshold;
            }

            ExchangeDepth book = null;//todo await api.GetDepthAsync(symbol, orderBookCount);
            if (book == null || (isBuy && book.Asks.Count == 0) || (!isBuy && book.Bids.Count == 0))
            {
                throw new APIException($"Error getting order book for {symbol}");
            }

            decimal counter = 0m;
            decimal highPrice = decimal.MinValue;
            decimal lowPrice = decimal.MaxValue;
            if (isBuy)
            {
                foreach (ExchangeOrderPrice ask in book.Asks.Values)
                {
                    counter += ask.Amount;
                    highPrice = Math.Max(highPrice, ask.Price);
                    lowPrice = Math.Min(lowPrice, ask.Price);
                    if (counter >= amount)
                    {
                        break;
                    }
                }
            }
            else
            {
                foreach (ExchangeOrderPrice bid in book.Bids.Values)
                {
                    counter += bid.Amount;
                    highPrice = Math.Max(highPrice, bid.Price);
                    lowPrice = Math.Min(lowPrice, bid.Price);
                    if (counter >= amount)
                    {
                        break;
                    }
                }
            }

            if (abortIfOrderBookTooSmall && counter < amount)
            {
                throw new APIException(
                    $"{(isBuy ? "Buy" : "Sell")} order for {symbol} and amount {amount} cannot be fulfilled because the order book is too thin.");
            }
            else if (lowPrice / highPrice < thresholdToAbort)
            {
                throw new APIException(
                    $"{(isBuy ? "Buy" : "Sell")} order for {symbol} and amount {amount} would place for a price below threshold of {thresholdToAbort}, aborting.");
            }

            ExchangeOrderRequest request = new ExchangeOrderRequest
            {
                Amount = amount,
                OrderType = OrderType.Limit,
                Price = CryptoUtility.RoundAmount((isBuy ? highPrice : lowPrice) * priceThreshold),
                ShouldRoundAmount = true,
                Symbol = null //todo symbol
            };
            ExchangeOrderResult result = await api.PlaceOrderAsync(request);

            // wait about 10 seconds until the order is fulfilled
            int i = 0;
            const int maxTries = 20; // 500 ms for each try
            for (; i < maxTries; i++)
            {
                await System.Threading.Tasks.Task.Delay(500);
                //TODO
                //result = await api.GetOrderDetailsAsync(result.OrderId, symbol);
                //switch (result.Result)
                //{
                //    case ExchangeAPIOrderResult.Filled:
                //    case ExchangeAPIOrderResult.Canceled:
                //    case ExchangeAPIOrderResult.Error:
                //        break;
                //}
            }

            if (i == maxTries)
            {
                throw new APIException(
                    $"{(isBuy ? "Buy" : "Sell")} order for {symbol} and amount {amount} timed out and may not have been fulfilled");
            }

            return result;
        }

        #region market

        #region depth

        /// <summary>Common order book parsing method, most exchanges use "asks" and "bids" with
        /// arrays of length 2 for price and amount (or amount and price)</summary>
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
                if (book.Asks.Count == maxCount)
                {
                    break;
                }
            }

            foreach (JArray array in token[bids])
            {
                var depth = new ExchangeOrderPrice
                {
                    Price = array[0].ConvertInvariant<decimal>(),
                    Amount = array[1].ConvertInvariant<decimal>()
                };
                book.Bids[depth.Price] = depth;
                if (book.Bids.Count == maxCount)
                {
                    break;
                }
            }

            return book;
        }

        /// <summary>Common order book parsing method, checks for "amount" or "quantity" and "price"
        /// elements</summary>
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
            foreach (JToken ask in token[asks])
            {
                var depth = new ExchangeOrderPrice
                {
                    Price = ask[price].ConvertInvariant<decimal>(),
                    Amount = ask[amount].ConvertInvariant<decimal>()
                };
                book.Asks[depth.Price] = depth;
                if (book.Asks.Count == maxCount)
                {
                    break;
                }
            }

            foreach (JToken bid in token[bids])
            {
                var depth = new ExchangeOrderPrice
                {
                    Price = bid[price].ConvertInvariant<decimal>(),
                    Amount = bid[amount].ConvertInvariant<decimal>()
                };
                book.Bids[depth.Price] = depth;
                if (book.Bids.Count == maxCount)
                {
                    break;
                }
            }

            return book;
        }


        #endregion

        /// <summary>
        /// Parse a JToken into a ticker
        /// </summary>
        /// <param name="token">Token</param>
        /// <param name="symbol"></param>
        /// <param name="askKey">Ask key</param>
        /// <param name="bidKey">Bid key</param>
        /// <param name="lastKey">Last key</param>
        /// <param name="baseVolumeKey">Base currency volume key</param>
        /// <param name="quoteVolumeKey">Quote currency volume key</param>
        /// <param name="timestampKey">Timestamp key</param>
        /// <param name="timestampType">Timestamp type</param>
        /// <param name="baseCurrencyKey">Base currency key</param>
        /// <param name="quoteCurrencyKey">Quote currency key</param>
        /// <param name="idKey">Id key</param>
        /// <returns>ExchangeTicker</returns>
        internal static ExchangeTicker ParseTicker(this JToken token, Symbol symbol,
            object askKey, object bidKey, object lastKey,
            object baseVolumeKey, object quoteVolumeKey = null,
            object timestampKey = null, TimestampType timestampType = TimestampType.None,
            object baseCurrencyKey = null, object quoteCurrencyKey = null,
            object idKey = null)
        {
            if (token == null || !token.HasValues)
            {
                return null;
            }

            decimal last = token[lastKey].ConvertInvariant<decimal>();

            // parse out volumes, handle cases where one or both do not exist
            token.ParseVolumes(baseVolumeKey, quoteVolumeKey, last,
                out decimal baseCurrencyVolume,
                out decimal quoteCurrencyVolume);

            // pull out timestamp
            DateTime timestamp = (timestampKey == null
                ? CryptoUtility.UtcNow
                : CryptoUtility.ParseTimestamp(token[timestampKey], timestampType));

            JToken askValue = null;
            if (askKey != null)
            {
                askValue = token[askKey];

                if (askValue is JArray)
                {
                    askValue = askValue[0];
                }
            }

            JToken bidValue = null;
            if (bidKey != null)
            {
                bidValue = token[bidKey];
                if (bidValue is JArray)
                {
                    bidValue = bidValue[0];
                }
            }

            ExchangeTicker ticker = new ExchangeTicker
            {
                Symbol = symbol,
                Ask = askValue.ConvertInvariant<decimal>(),
                Bid = bidValue.ConvertInvariant<decimal>(),
                Id = (idKey == null ? null : token[idKey].ToStringInvariant()),
                Last = last,

                BaseCurrencyVolume = baseCurrencyVolume,
                QuoteCurrencyVolume = quoteCurrencyVolume,
                Timestamp = timestamp

            };

            return ticker;
        }

        /// <summary>
        /// Parse a trade
        /// </summary>
        /// <param name="token">Token</param>
        /// <param name="amountKey">Amount key</param>
        /// <param name="priceKey">Price key</param>
        /// <param name="typeKey">Type key</param>
        /// <param name="timestampKey">Timestamp key</param>
        /// <param name="timestampType">Timestamp type</param>
        /// <param name="idKey">Id key</param>
        /// <param name="typeKeyIsBuyValue">Type key buy value</param>
        /// <returns>Trade</returns>
        internal static ExchangeTrade ParseTrade(this JToken token, object amountKey, object priceKey, object typeKey,
            object timestampKey, TimestampType timestampType, object idKey = null, string typeKeyIsBuyValue = "buy")
        {
            ExchangeTrade trade = new ExchangeTrade
            {
                Amount = token[amountKey].ConvertInvariant<decimal>(),
                Price = token[priceKey].ConvertInvariant<decimal>(),
                IsBuy = (token[typeKey].ToStringInvariant().EqualsWithOption(typeKeyIsBuyValue)),
                Timestamp = (timestampKey == null
                    ? CryptoUtility.UtcNow
                    : CryptoUtility.ParseTimestamp(token[timestampKey], timestampType))
            };

            trade.Id = idKey == null ? trade.Timestamp.Ticks.ToString() : token[idKey].ToStringInvariant();

            return trade;
        }

        /// <summary>
        /// Parse volume from JToken
        /// </summary>
        /// <param name="token">JToken</param>
        /// <param name="baseVolumeKey">Base currency volume key</param>
        /// <param name="quoteVolumeKey">Quote currency volume key</param>
        /// <param name="last">Last volume value</param>
        /// <param name="baseCurrencyVolume">Receive base currency volume</param>
        /// <param name="quoteCurrencyVolume">Receive quote currency volume</param>
        internal static void ParseVolumes(this JToken token, object baseVolumeKey, object quoteVolumeKey, decimal last,
            out decimal baseCurrencyVolume, out decimal quoteCurrencyVolume)
        {
            // parse out volumes, handle cases where one or both do not exist
            if (baseVolumeKey == null)
            {
                if (quoteVolumeKey == null)
                {
                    baseCurrencyVolume = quoteCurrencyVolume = 0m;
                }
                else
                {
                    quoteCurrencyVolume = token[quoteVolumeKey].ConvertInvariant<decimal>();
                    baseCurrencyVolume = (last <= 0m ? 0m : quoteCurrencyVolume / last);
                }
            }
            else
            {
                baseCurrencyVolume = token[baseVolumeKey].ConvertInvariant<decimal>();
                if (quoteVolumeKey == null)
                {
                    quoteCurrencyVolume = baseCurrencyVolume * last;
                }
                else
                {
                    quoteCurrencyVolume = token[quoteVolumeKey].ConvertInvariant<decimal>();
                }
            }
        }

        /// <summary>
        /// Parse market candle from JArray
        /// </summary>
        /// <param name="token">JToken</param>
        /// <param name="symbol"></param>
        /// <param name="periodSeconds">Period seconds</param>
        /// <param name="openKey">Open key</param>
        /// <param name="highKey">High key</param>
        /// <param name="lowKey">Low key</param>
        /// <param name="closeKey">Close key</param>
        /// <param name="timestampKey">Timestamp key</param>
        /// <param name="timestampType">Timestamp type</param>
        /// <param name="baseVolumeKey">Base currency volume key</param>
        /// <param name="quoteVolumeKey">Quote currency volume key</param>
        /// <param name="weightedAverageKey">Weighted average key</param>
        /// <returns>MarketCandle</returns>
        internal static MarketCandle ParseCandle(this JToken token, Symbol symbol, int periodSeconds,
            object openKey, object highKey, object lowKey,
            object closeKey, object timestampKey, TimestampType timestampType, object baseVolumeKey,
            object quoteVolumeKey = null, object weightedAverageKey = null)
        {
            MarketCandle candle = new MarketCandle
            {
                Symbol = symbol,
                ClosePrice = token[closeKey].ConvertInvariant<decimal>(),
                HighPrice = token[highKey].ConvertInvariant<decimal>(),
                LowPrice = token[lowKey].ConvertInvariant<decimal>(),
                OpenPrice = token[openKey].ConvertInvariant<decimal>(),
                PeriodSeconds = periodSeconds,
                Timestamp = CryptoUtility.ParseTimestamp(token[timestampKey], timestampType)
            };

            token.ParseVolumes(
                baseVolumeKey, quoteVolumeKey, candle.ClosePrice, 
                out decimal baseVolume, out decimal convertVolume);

            candle.BaseCurrencyVolume = (double) baseVolume;
            candle.QuoteCurrencyVolume = (double) convertVolume;

            if (weightedAverageKey != null)
            {
                candle.WeightedAverage = token[weightedAverageKey].ConvertInvariant<decimal>();
            }

            return candle;
        }

        #endregion
    }
}