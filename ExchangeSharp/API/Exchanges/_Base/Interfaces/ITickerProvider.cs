using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExchangeSharp
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

    }
}
