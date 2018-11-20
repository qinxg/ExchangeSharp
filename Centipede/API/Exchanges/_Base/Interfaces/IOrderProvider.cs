using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Centipede
{


    public class ExchangeOrderCancelRequest
    {
        public  Symbol Symbol { get; set; }

        public  string[] OrderIds { get; set; }
    }


    /// <summary>
    /// 订单
    /// </summary>
    public interface IOrderProvider
    {

        /// <summary>
        /// 批量提交订单 （只有okex是真正支持的）
        /// </summary>
        Task<List<ExchangeOrderResult>> PlaceOrdersAsync(params ExchangeOrderRequest[] orders);


        /// <summary>
        /// 取消订单
        /// Cancel an order, an exception is thrown if failure
        /// </summary>
        /// <param name="orders"></param>
        Task CancelOrdersAsync(params ExchangeOrderCancelRequest[] orders);

        /// <summary>
        /// 获取所有未完成的订单详情
        /// </summary>
        Task<IEnumerable<ExchangeOrderResult>> GetOpenOrderDetailsAsync(Symbol symbol = null);

        /// <summary>
        /// 获取所有完成的订单详情 (部分成交撤销、已撤销、完全成交）
        /// Get the details of all completed orders
        /// </summary>
        /// <param name="symbol">Market symbol to get completed orders for or null for all</param>
        /// <param name="afterDate">Only returns orders on or after the specified date/time</param>
        /// <returns>All completed order details for the specified symbol, or all if null symbol</returns>
        Task<IEnumerable<ExchangeOrderResult>> GetCompletedOrderDetailsAsync(Symbol symbol = null,
            DateTime? afterDate = null);

    }
}
