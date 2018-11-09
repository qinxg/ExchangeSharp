using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// 深度信息
    /// </summary>
    public interface IOrderBookProvider
    {
        /// <summary>
        /// Get pending orders. Depending on the exchange, the number of bids and asks will have different counts, typically 50-100.
        /// </summary>
        /// <param name="marketSymbol">Symbol</param>
        /// <param name="maxCount">Max count of bids and asks - not all exchanges will honor this parameter</param>
        /// <returns>Orders</returns>
        Task<ExchangeOrderBook> GetOrderBookAsync(string marketSymbol, int maxCount = 100);

        /// <summary>
        /// Get exchange order book for all symbols. Not all exchanges support this. Depending on the exchange, the number of bids and asks will have different counts, typically 50-100.
        /// </summary>
        /// <param name="maxCount">Max count of bids and asks - not all exchanges will honor this parameter</param>
        /// <returns>Symbol and order books pairs</returns>
        Task<IEnumerable<KeyValuePair<string, ExchangeOrderBook>>> GetOrderBooksAsync(int maxCount = 100);

        /// <summary>
        /// Get order book over web socket. This behaves differently depending on WebSocketOrderBookType.
        /// </summary>
        /// <param name="callback">Callback with the full ExchangeOrderBook</param>
        /// <param name="maxCount">Max count of bids and asks - not all exchanges will honor this parameter</param>
        /// <param name="marketSymbols">Market symbols or null/empty for all of them (if supported)</param>
        /// <returns>Web socket, call Dispose to close</returns>
        IWebSocket GetOrderBookWebSocket(Action<ExchangeOrderBook> callback, int maxCount = 20, params string[] marketSymbols);

        /// <summary>
        /// What type of web socket order book is provided
        /// </summary>
        WebSocketOrderBookType WebSocketOrderBookType { get; }
    }

    /// <summary>
    /// Web socket order book type
    /// </summary>
    public enum WebSocketOrderBookType
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
