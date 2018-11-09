using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExchangeSharp
{
    /// <summary>
    /// 订单
    /// </summary>
    public interface IOrderProvider
    {

        /// <summary>
        /// 提交订单
        /// Place an order
        /// </summary>
        /// <param name="order">Order request</param>
        /// <returns>Order result and message string if any</returns>
        Task<ExchangeOrderResult> PlaceOrderAsync(ExchangeOrderRequest order);

        /// <summary>
        /// Place bulk orders
        /// </summary>
        /// <param name="orders">Order requests</param>
        /// <returns>Order results, each result matches up with each order in index</returns>
        Task<ExchangeOrderResult[]> PlaceOrdersAsync(params ExchangeOrderRequest[] orders);

        /// <summary>
        /// 获取订单详情
        /// Get details of an order
        /// </summary>
        /// <param name="orderId">order id</param>
        /// <param name="marketSymbol">Market Symbol</param>
        /// <returns>Order details</returns>
        Task<ExchangeOrderResult> GetOrderDetailsAsync(string orderId, string marketSymbol = null);

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

        /// <summary>
        /// 取消订单
        /// Cancel an order, an exception is thrown if failure
        /// </summary>
        /// <param name="orderId">Order id of the order to cancel</param>
        /// <param name="marketSymbol">Market symbol of the order to cancel (not required for most exchanges)</param>
        Task CancelOrderAsync(string orderId, string marketSymbol = null);

        /// <summary>
        /// 获取可用于交易的保证金金额
        /// Get margin amounts available to trade, symbol / amount dictionary
        /// </summary>
        /// <param name="includeZeroBalances">Include currencies with zero balance in return value</param>
        /// <returns>Dictionary of symbols and amounts available to trade in margin account</returns>
        Task<Dictionary<string, decimal>> GetMarginAmountsAvailableToTradeAsync(bool includeZeroBalances = false);

        /// <summary>
        /// 获取未平仓的款项
        /// Get open margin position
        /// </summary>
        /// <param name="marketSymbol">Market Symbol</param>
        /// <returns>Open margin position result</returns>
        Task<ExchangeMarginPositionResult> GetOpenPositionAsync(string marketSymbol);

        /// <summary>
        /// 平仓
        /// Close a margin position
        /// </summary>
        /// <param name="marketSymbol">Market Symbol</param>
        /// <returns>Close margin position result</returns>
        Task<ExchangeCloseMarginPositionResult> CloseMarginPositionAsync(string marketSymbol);
    }
}
