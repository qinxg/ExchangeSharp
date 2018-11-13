using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// K线信息
    /// </summary>
    public interface IKlineProvider
    {
        /// <summary>
        /// K线数据， 开，最高，最低，收盘
        /// Get candles (open, high, low, close)
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="periodSeconds"> K线类型
        ///     Period in seconds to get candles for.
        ///     Use 60 for minute, 3600 for hour, 3600*24 for day, 3600*24*30 for month.</param>
        /// <param name="startDate">Optional start date to get candles for</param>
        /// <param name="endDate">Optional end date to get candles for</param>
        /// <param name="limit">Max results, can be used instead of startDate and endDate if desired</param>
        /// <returns>Candles</returns>
        Task<List<MarketCandle>> GetCandlesAsync(Symbol symbol, int periodSeconds,
            DateTime? startDate = null, DateTime? endDate = null, int? limit = null);

    }
}
