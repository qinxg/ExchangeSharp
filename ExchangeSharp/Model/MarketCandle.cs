using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// 市场k线信息
    /// Candlestick data
    /// </summary>
    public class MarketCandle
    {
        /// <summary>
        /// The name of the exchange for this candle
        /// </summary>
        public string ExchangeName { get; set; }

        /// <summary>
        /// The name of the market
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Timestamp, the open time of the candle
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The period in seconds
        /// </summary>
        public int PeriodSeconds { get; set; }

        /// <summary>
        /// Opening price
        /// </summary>
        public decimal OpenPrice { get; set; }

        /// <summary>
        /// High price
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// Low price
        /// </summary>
        public decimal LowPrice { get; set; }

        /// <summary>
        /// Close price
        /// </summary>
        public decimal ClosePrice { get; set; }

        /// <summary>
        /// Base currency volume (i.e. in BTC-USD, this would be BTC volume)
        /// </summary>
        public double BaseCurrencyVolume { get; set; }

        /// <summary>
        /// Quote currency volume (i.e. in BTC-USD, this would be USD volume)
        /// </summary>
        public double QuoteCurrencyVolume { get; set; }

        /// <summary>
        /// The weighted average price if provided
        /// </summary>
        public decimal WeightedAverage { get; set; }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns>String</returns>
        public override string ToString()
        {
            return
                $"{Timestamp}/{PeriodSeconds}: {OpenPrice}, {HighPrice}, {LowPrice}, {ClosePrice}, {BaseCurrencyVolume}, {QuoteCurrencyVolume}, {WeightedAverage}";
        }
    }
}
