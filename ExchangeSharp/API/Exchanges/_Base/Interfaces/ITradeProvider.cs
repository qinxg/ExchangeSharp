using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// 成交记录信息
    /// </summary>
    public interface ITradeProvider
    {
        /// <summary>
        /// 获取历史交易情况
        /// Get historical trades for the exchange
        /// </summary>
        /// <param name="callback">Callback for each set of trades. Return false to stop getting trades immediately.</param>
        /// <param name="marketSymbol">Symbol to get historical data for</param>
        /// <param name="startDate">Optional start date time to start getting the historical data at, null for the most recent data. Not all exchanges support this.</param>
        /// <param name="endDate">Optional UTC end date time to start getting the historical data at, null for the most recent data. Not all exchanges support this.</param>
        Task GetHistoricalTradesAsync(Func<IEnumerable<ExchangeTrade>, bool> callback, string marketSymbol,
            DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 最后交易情况
        /// Get the latest trades
        /// </summary>
        /// <param name="marketSymbol">Market Symbol</param>
        /// <returns>Trades</returns>
        Task<IEnumerable<ExchangeTrade>> GetRecentTradesAsync(string marketSymbol);


        /// <summary>
        /// 交易数据
        /// Get information about trades via web socket
        /// </summary>
        /// <param name="callback">Callback (symbol and trade)</param>
        /// <param name="marketSymbols">Market symbols</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetTradesWebSocket(Action<KeyValuePair<string, ExchangeTrade>> callback,
            params string[] marketSymbols);


    }
}
