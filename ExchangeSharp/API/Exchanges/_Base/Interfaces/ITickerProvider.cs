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
        /// 获取最后行情数据
        /// Get latest ticker
        /// </summary>
        /// <param name="marketSymbol">Symbol</param>
        /// <returns>Latest ticker</returns>
        Task<ExchangeTicker> GetTickerAsync(string marketSymbol);

        /// <summary>
        /// 获取所有行情数据，不是所有交易所都支持
        /// Get all tickers, not all exchanges support this
        /// </summary>
        /// <returns>Key value pair of symbol and tickers array</returns>
        Task<IEnumerable<KeyValuePair<string, ExchangeTicker>>> GetTickersAsync();


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
