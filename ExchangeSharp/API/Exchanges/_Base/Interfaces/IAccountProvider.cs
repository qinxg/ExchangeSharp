using System.Collections.Generic;
using System.Threading.Tasks;

namespace Centipede
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
    }
}
