using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeSharp
{
    /// <summary>
    /// 交易所信息
    /// Information about an exchange
    /// </summary>
    public sealed class ExchangeInfo
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="api">Exchange API</param>
        /// <param name="marketSymbol">The market symbol to trade by default, can be null</param>
        public ExchangeInfo(IExchangeAPI api, string marketSymbol = null)
        {
            API = api;
            MarketSymbols = api.GetMarketSymbolsAsync().Sync().ToArray();
            TradeInfo = new ExchangeTradeInfo(this, marketSymbol);
        }

        /// <summary>
        /// Update the exchange info - get new trade info, etc.
        /// </summary>
        public void Update()
        {
            TradeInfo.Update();
        }

        /// <summary>
        /// API to interact with the exchange
        /// </summary>
        public IExchangeAPI API { get; private set; }

        /// <summary>
        /// User assigned identifier of the exchange, can be left at zero if not needed
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 交易所支持的币对
        /// Market symbols of the exchange
        /// </summary>
        public IReadOnlyCollection<string> MarketSymbols { get; private set; }

        /// <summary>
        /// Latest trade info for the exchange
        /// </summary>
        public ExchangeTradeInfo TradeInfo { get; private set; }
    }
}
