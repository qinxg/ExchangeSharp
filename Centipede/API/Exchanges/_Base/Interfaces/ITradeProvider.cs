﻿using System;
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
        /// 交易数据
        /// Get the latest trades
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="limit"></param>
        /// <returns>Trades</returns>
        Task<List<ExchangeTrade>> GetTradesAsync(Symbol symbol, int limit = 20);


        /// <summary>
        /// 交易数据
        /// Get information about trades via web socket
        /// </summary>
        /// <param name="callback">Callback (symbol and trade)</param>
        /// <param name="symbols">Market symbols</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetTradesWebSocket(Action<List<ExchangeTrade>> callback, params Symbol[] symbols);

    }
}
