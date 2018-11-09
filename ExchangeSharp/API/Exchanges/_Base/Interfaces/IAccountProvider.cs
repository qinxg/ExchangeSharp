using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExchangeSharp
{
    /// <summary>
    /// 账户
    /// </summary>
    public interface IAccountProvider
    {

        /// <summary>
        /// 获取货币数量
        /// Get total amounts, symbol / amount dictionary
        /// </summary>
        /// <returns>Dictionary of symbols and amounts</returns>
        Task<Dictionary<string, decimal>> GetAmountsAsync();

        /// <summary>
        /// 可交易的货币数量
        /// Get amounts available to trade, symbol / amount dictionary
        /// </summary>
        /// <returns>Dictionary of symbols and amounts available to trade</returns>
        Task<Dictionary<string, decimal>> GetAmountsAvailableToTradeAsync();
    }
}
