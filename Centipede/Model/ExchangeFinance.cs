using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Centipede
{

    /// <summary>
    /// 交易所账户资产情况
    /// </summary>
    public class ExchangeFinance
    {
        public  Currency Currency { get; set; }
        
        /// <summary>
        /// 余额
        /// </summary>
        public  decimal Balance { get; set; }

        /// <summary>
        /// 冻结
        /// </summary>
        public decimal Hold { get; set; }

        /// <summary>
        /// 可用
        /// </summary>
        public decimal Available { get; set; }

    }
}
