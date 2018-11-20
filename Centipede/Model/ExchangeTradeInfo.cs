using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// Latest trade info for an exchange
    /// </summary>
    public sealed class ExchangeTradeInfo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">Exchange info</param>
        /// <param name="marketSymbol">The symbol to trade</param>
        public ExchangeTradeInfo(ExchangeInfo info, string marketSymbol)
        {
            ExchangeInfo = info;
            MarketSymbol = marketSymbol;
        }

        /// <summary>
        /// Update the trade info via API
        /// </summary>
        public void Update()
        {
            //todo
            //Ticker = ExchangeInfo.API.GetTickerAsync(Symbol).Sync();
            //RecentTrades = ExchangeInfo.API.GetTradesAsync(Symbol).Sync().ToArray();
            //if (RecentTrades.Length == 0)
            //{
            //    Trade = new Trade();
            //}
            //else
            //{
            //    Trade = new Trade { Amount = (float)RecentTrades[RecentTrades.Length - 1].Amount, Price = (float)RecentTrades[RecentTrades.Length - 1].Price, Ticks = (long)CryptoUtility.UnixTimestampFromDateTimeMilliseconds(RecentTrades[RecentTrades.Length - 1].DateTime) };
            //}
            //Orders = ExchangeInfo.API.GetDepthAsync(Symbol).Sync();
        }

        /// <summary>
        /// Exchange info
        /// </summary>
        public ExchangeInfo ExchangeInfo { get; private set; }

        /// <summary>
        /// Ticker for the exchange
        /// </summary>
        public ExchangeTicker Ticker { get; private set; }

        /// <summary>
        /// Recent trades in ascending order
        /// </summary>
        public ExchangeTrade[] RecentTrades { get; private set; }

        /// <summary>
        /// Pending orders on the exchange
        /// </summary>
        public ExchangeDepth Orders { get; private set; }

        /// <summary>
        /// The last trade made, allows setting to facilitate fast testing of traders based on price alone
        /// </summary>
        public Trade Trade { get; set; }

        /// <summary>
        /// The current market symbol being traded
        /// </summary>
        public string MarketSymbol { get; set; }
    }
}
