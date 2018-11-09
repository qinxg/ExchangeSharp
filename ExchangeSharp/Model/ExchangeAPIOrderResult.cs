/*

*/

namespace ExchangeSharp
{
    
    /// <summary>
    /// 订单结果
    /// </summary>
    public enum ExchangeAPIOrderResult
    {
        /// <summary>
        /// 未知订单状态
        /// </summary>
        Unknown,

        /// <summary>
        /// 完全成交 
        /// </summary>
        Filled,

        /// <summary>
        /// 部分成交
        /// </summary>
        FilledPartially,

        /// <summary>
        /// 未成交
        /// </summary>
        Pending,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 已撤销
        /// </summary>
        Canceled,

        /// <summary>
        /// 撤单处理中 （这个用的比较少）
        /// </summary>
        PendingCancel,
    }
}