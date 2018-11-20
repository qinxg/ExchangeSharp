using System;
using System.IO;

namespace Centipede
{
    /// <summary>
    /// 行情数据
    /// Details of the current price of an exchange asset
    /// </summary>
    public sealed class ExchangeTicker
    {
        /// <summary>
        /// An exchange specific id if known, otherwise null
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 币对情况
        /// </summary>
        public  Symbol Symbol { get; set; }

        /// <summary>
        /// The bid is the price to sell at
        /// </summary>
        public decimal? Bid { get; set; }

        /// <summary>
        /// The ask is the price to buy at
        /// </summary>
        public decimal? Ask { get; set; }

        /// <summary>
        /// The last trade purchase price
        /// </summary>
        public decimal Last { get; set; }


        /// <summary>
        /// Last volume update timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }


        /// <summary>
        /// 计价货币数量
        /// Amount in units of the QuoteCurrency - will equal BaseCurrencyVolume if exchange doesn't break it out by price unit and quantity unit
        /// In BTC-USD, this would be USD volume
        /// </summary>
        public decimal QuoteCurrencyVolume { get; set; }

        /// <summary>
        /// 基础货币数量
        /// Base currency amount (this many units total)
        /// In BTC-USD this would be BTC volume
        /// </summary>
        public decimal BaseCurrencyVolume { get; set; }

        /// <summary>
        /// Get a string for this ticker
        /// </summary>
        /// <returns>String</returns>
        public override string ToString()
        {
            return $"Bid: {Bid}, Ask: {Ask}, Last: {Last}";
        }

    }
}
