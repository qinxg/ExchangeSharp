/*

*/

namespace ExchangeSharp
{
    /// <summary>
    /// 指定信息汇总
    /// A summary of a specific market/asset
    /// </summary>
    public class MarketSummary
    {
        /// <summary>
        /// The name of the exchange for the market
        /// </summary>
        public string ExchangeName { get; set; }

        /// <summary>
        /// The name of the market
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The last price paid for the asset of the market
        /// </summary>
        public decimal LastPrice { get; set; }
        
        /// <summary>
        /// The highest price paid for the asset of the market, usually in the last 24hr
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// The lowest price paid for the asset of the market, usually in the last 24hr
        /// </summary>
        public decimal LowPrice { get; set; }

        /// <summary>
        /// The percent change in price, usually in the last 24hr
        /// </summary>
        public double PriceChangePercent { get; set; }

        /// <summary>
        /// The absolute change in price, usually in the last 24hr
        /// </summary>
        public decimal PriceChangeAmount { get; set; }

        /// <summary>
        /// The volume, usually in the last 24hr
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// The percent change in volume, usually in the last 24hr
        /// </summary>
        public double VolumeChangePercent { get; set; }

        /// <summary>
        /// The absolute change in volume, usually in the last 24hr
        /// </summary>
        public double VolumeChangeAmount { get; set; }
    }
}
