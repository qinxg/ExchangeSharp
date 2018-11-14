using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// 深度信息
    /// </summary>
    public interface IDepthProvider
    {
        /// <summary>
        /// Get pending orders. Depending on the exchange, the number of bids and asks will have different counts, typically 50-100.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>Orders</returns>
        Task<ExchangeDepth> GetDepthAsync(Symbol symbol, int maxCount);

        /// <summary>
        /// Get order book over web socket. This behaves differently depending on WebSocketDepthType.
        /// </summary>
        /// <param name="callback">Callback with the full ExchangeDepth</param>
        /// <param name="maxCount">Max count of bids and asks - not all exchanges will honor this parameter</param>
        /// <param name="symbols">Market symbols or null/empty for all of them (if supported)</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetDepthWebSocket(Action<ExchangeDepth> callback, int maxCount = 20, params Symbol[] symbols);

        /// <summary>
        /// What type of web socket order book is provided
        /// </summary>
        WebSocketDepthType WebSocketDepthType { get; }
    }

    /// <summary>
    /// Web socket order book type
    /// </summary>
    public enum WebSocketDepthType
    {
        /// <summary>
        /// Web socket order book not supported
        /// </summary>
        None,

        /// <summary>
        /// Web socket order book sends full book upon connect, and then delta books
        /// </summary>
        FullBookFirstThenDeltas,

        /// <summary>
        /// Web socket order book sends only delta books
        /// </summary>
        DeltasOnly,

        /// <summary>
        /// Web socket order book sends the full book always
        /// </summary>
        FullBookAlways
    }
}
