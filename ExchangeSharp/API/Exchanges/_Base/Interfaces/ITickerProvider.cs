using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// 行情信息
    /// </summary>
    public interface ITickerProvider
    {
        /// <summary>
        /// 获取单个币对的行情信息
        /// Get latest ticker
        /// </summary>
        /// <returns>Latest ticker</returns>
        Task<ExchangeTicker> GetTickerAsync(Symbol symbol);

        /// <summary>
        /// 获取所有行情数据，不是所有交易所都支持
        /// Get all tickers, not all exchanges support this
        /// </summary>
        /// <returns>Key value pair of symbol and tickers array</returns>
        Task<List<ExchangeTicker>> GetTickersAsync();


        /// <summary>
        /// 所有行情数据
        /// Get all tickers via web socket
        /// </summary>
        /// <param name="callback">Callback</param>
        /// <param name="symbols">Symbols. If no symbols are specified, this will get the tickers for all symbols. NOTE: Some exchanges don't allow you to specify which symbols to return.</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetTickersWebSocket(Action<IReadOnlyCollection<KeyValuePair<string, ExchangeTicker>>> callback,
            params string[] symbols);

    }
}
