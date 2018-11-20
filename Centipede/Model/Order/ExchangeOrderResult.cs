using System;

namespace Centipede
{


    /// <summary> 订单结果
    /// Result of an exchange order</summary>
    public sealed class ExchangeOrderResult
    {
        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// 订单结果
        /// </summary>
        public ExchangeAPIOrderResult Result { get; set; }


       /// <summary>
       /// 订单数量
       /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        ///下单价格
        /// </summary>
        public decimal Price { get; set; }


        /// <summary>
        /// 订单创建时间
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// 已成交数量
        /// </summary>
        public decimal AmountFilled { get; set; }


        /// <summary>
        /// 交易对
        /// </summary>
        public Symbol Symbol { get; set; }
        
        
        /// <summary>
        /// 是否为买入
        /// </summary>
        public bool IsBuy { get; set; }

        /// <summary>手续费</summary>
        public decimal Fees { get; set; }
    }
}
