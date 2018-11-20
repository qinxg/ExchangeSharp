using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// This shows all the methods that can be overriden when implementation a new exchange, along
    /// with all the fields that should be set in the constructor or static constructor if needed.
    /// </summary>
    public abstract partial class ExchangeAPI
    {       
        /// <summary>
        /// Websocket深度信息的类型
        /// 例如：一开始是全集，后面增量、全部是增量、全部是全集 等
        /// The type of web socket order book supported
        /// </summary>
        public WebSocketDepthType WebSocketDepthType { get; protected set; } = WebSocketDepthType.None;
    }
}
