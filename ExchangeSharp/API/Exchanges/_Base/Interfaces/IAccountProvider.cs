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
        /// 资产情况
        /// </summary>
        Task<List<ExchangeFinance>> GetFinanceAsync();
    }
}
