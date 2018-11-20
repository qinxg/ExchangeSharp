using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;using Newtonsoft.Json.Linq;

namespace Centipede
{
    public partial class ExchangeAPI
    {
        protected class ExchangeHistoricalTradeHelper
        {
            private ExchangeAPI api;

            public Func<List<ExchangeTrade>, bool> Callback { get; set; }
            public string MarketSymbol { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string Url { get; set; } // url with format [marketSymbol], {0} = start timestamp, {1} = end timestamp
            public int DelayMilliseconds { get; set; } = 1000;
            public TimeSpan BlockTime { get; set; } = TimeSpan.FromHours(1.0); // how much time to move for each block of data, default 1 hour
            public bool MillisecondGranularity { get; set; } = true;
            public Func<DateTime, string> TimestampFunction { get; set; } // change date time to a url timestamp, use TimestampFunction or UrlFunction
            public Func<ExchangeHistoricalTradeHelper, string> UrlFunction { get; set; } // allows returning a custom url, use TimestampFunction or UrlFunction
            public Func<JToken, ExchangeTrade> ParseFunction { get; set; }
            public bool DirectionIsBackwards { get; set; } = true; // some exchanges support going from most recent to oldest, but others, like Gemini must go from oldest to newest

            public ExchangeHistoricalTradeHelper(ExchangeAPI api)
            {
                this.api = api;
            }

            public async Task ProcessHistoricalTrades()
            {
                if (Callback == null)
                {
                    throw new ArgumentException("Missing required parameter", nameof(Callback));
                }
                else if (TimestampFunction == null && UrlFunction == null)
                {
                    throw new ArgumentException("Missing required parameters", nameof(TimestampFunction) + "," + nameof(UrlFunction));
                }
                else if (ParseFunction == null)
                {
                    throw new ArgumentException("Missing required parameter", nameof(ParseFunction));
                }
                else if (string.IsNullOrWhiteSpace(Url))
                {
                    throw new ArgumentException("Missing required parameter", nameof(Url));
                }

                string url;
                Url = Url.Replace("[marketSymbol]", MarketSymbol);
                List<ExchangeTrade> trades = new List<ExchangeTrade>();
                ExchangeTrade trade;
                EndDate = (EndDate ?? CryptoUtility.UtcNow);
                StartDate = (StartDate ?? EndDate.Value.Subtract(BlockTime));
                string startTimestamp;
                string endTimestamp;
                HashSet<string> previousTrades = new HashSet<string>();
                HashSet<string> tempTradeIds = new HashSet<string>();
                HashSet<string> tmpIds;
                SetDates(out DateTime startDateMoving, out DateTime endDateMoving);

                while (true)
                {
                    // format url and make request
                    if (TimestampFunction != null)
                    {
                        startTimestamp = TimestampFunction(startDateMoving);
                        endTimestamp = TimestampFunction(endDateMoving);
                        url = string.Format(Url, startTimestamp, endTimestamp);
                    }
                    else if (UrlFunction != null)
                    {
                        url = UrlFunction(this);
                    }
                    else
                    {
                        throw new InvalidOperationException("TimestampFunction or UrlFunction must be specified");
                    }
                    JToken obj = await api.MakeJsonRequestAsync<JToken>(url);

                    // don't add this temp trade as it may be outside of the date/time range
                    tempTradeIds.Clear();
                    foreach (JToken token in obj)
                    {
                        trade = ParseFunction(token);
                        if (!previousTrades.Contains(trade.Id) && trade.DateTime >= StartDate.Value &&
                            trade.DateTime <= EndDate.Value)
                        {
                            trades.Add(trade);
                        }

                        if (!string.IsNullOrEmpty(trade.Id))
                        {
                            tempTradeIds.Add(trade.Id);
                        }
                    }

                    previousTrades.Clear();
                    tmpIds = previousTrades;
                    previousTrades = tempTradeIds;
                    tempTradeIds = previousTrades;

                    // set dates to next block
                    if (trades.Count == 0)
                    {
                        if (DirectionIsBackwards)
                        {
                            // no trades found, move the whole block back
                            endDateMoving = startDateMoving.Subtract(BlockTime);
                        }
                        else
                        {
                            // no trades found, move the whole block forward
                            startDateMoving = endDateMoving.Add(BlockTime);
                        }
                    }
                    else
                    {
                        // sort trades in descending order and callback
                        if (DirectionIsBackwards)
                        {
                            trades.Sort((t1, t2) => t2.DateTime.CompareTo(t1.DateTime));
                        }
                        else
                        {
                            trades.Sort((t1, t2) => t1.DateTime.CompareTo(t2.DateTime));
                        }
                        if (!Callback(trades))
                        {
                            break;
                        }

                        trade = trades[trades.Count - 1];
                        if (DirectionIsBackwards)
                        {
                            // set end date to the date of the earliest trade of the block, use for next request
                            if (MillisecondGranularity)
                            {
                                endDateMoving = trade.DateTime.AddMilliseconds(-1.0);
                            }
                            else
                            {
                                endDateMoving = trade.DateTime.AddSeconds(-1.0);
                            }
                            startDateMoving = endDateMoving.Subtract(BlockTime);
                        }
                        else
                        {
                            // set start date to the date of the latest trade of the block, use for next request
                            if (MillisecondGranularity)
                            {
                                startDateMoving = trade.DateTime.AddMilliseconds(1.0);
                            }
                            else
                            {
                                startDateMoving = trade.DateTime.AddSeconds(1.0);
                            }
                            endDateMoving = startDateMoving.Add(BlockTime);
                        }
                        trades.Clear();
                    }
                    // check for exit conditions
                    if (DirectionIsBackwards)
                    {
                        if (endDateMoving < StartDate.Value)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (startDateMoving > EndDate.Value)
                        {
                            break;
                        }
                    }
                    ClampDates(ref startDateMoving, ref endDateMoving);
                    await Task.Delay(DelayMilliseconds);
                }
            }

            private void SetDates(out DateTime startDateMoving, out DateTime endDateMoving)
            {
                if (DirectionIsBackwards)
                {
                    endDateMoving = EndDate.Value;
                    startDateMoving = endDateMoving.Subtract(BlockTime);
                }
                else
                {
                    startDateMoving = StartDate.Value;
                    endDateMoving = startDateMoving.Add(BlockTime);
                }
                ClampDates(ref startDateMoving, ref endDateMoving);
            }

            private void ClampDates(ref DateTime startDateMoving, ref DateTime endDateMoving)
            {
                if (DirectionIsBackwards)
                {
                    if (startDateMoving < StartDate.Value)
                    {
                        startDateMoving = StartDate.Value;
                    }
                }
                else
                {
                    if (endDateMoving > EndDate.Value)
                    {
                        endDateMoving = EndDate.Value;
                    }
                }
            }
        }
    }
}
