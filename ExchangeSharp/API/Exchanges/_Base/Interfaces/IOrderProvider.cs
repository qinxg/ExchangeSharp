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
        /// <param name="orders">Order requests</param>
        Task<List<ExchangeOrderResult>> PlaceOrdersAsync(params ExchangeOrderRequest[] orders);


        /// <summary>
        /// 取消订单
        /// Cancel an order, an exception is thrown if failure
        /// </summary>
        /// <param name="orders"></param>
        Task CancelOrdersAsync(params ExchangeOrderCancelRequest[] orders);

        /// <summary>
        /// 获取订单详情
        /// Get details of an order
        /// </summary>
        /// <param name="orderId">order id</param>
        /// <param name="symbol">Market Symbol</param>
        /// <returns>Order details</returns>
        Task<ExchangeOrderResult> GetOrderDetailsAsync(string orderId, Symbol symbol = null);

        /// <summary>
        /// 获取所有未完成的订单详情
        /// Get the details of all open orders
        /// </summary>
        /// <param name="marketSymbol">Market symbol to get open orders for or null for all</param>
        /// <returns>All open order details for the specified symbol</returns>
        Task<IEnumerable<ExchangeOrderResult>> GetOpenOrderDetailsAsync(string marketSymbol = null);

        /// <summary>
        /// 获取所有完成的订单详情
        /// Get the details of all completed orders
        /// </summary>
        /// <param name="marketSymbol">Market symbol to get completed orders for or null for all</param>
        /// <param name="afterDate">Only returns orders on or after the specified date/time</param>
        /// <returns>All completed order details for the specified symbol, or all if null symbol</returns>
        Task<IEnumerable<ExchangeOrderResult>> GetCompletedOrderDetailsAsync(string marketSymbol = null,
            DateTime? afterDate = null);

    }
}
