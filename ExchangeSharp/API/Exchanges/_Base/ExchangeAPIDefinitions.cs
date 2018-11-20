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
        /// Dictionary of key (exchange currency) and value (global currency). Add entries in static constructor.
        /// Some exchanges (Yobit for example) use odd names for some currencies like BCC for Bitcoin Cash.
        /// <example><![CDATA[ 
        /// ExchangeGlobalCurrencyReplacements[typeof(ExchangeYobitAPI)] = new KeyValuePair<string, string>[]
        /// {
        ///     new KeyValuePair<string, string>("BCC", "BCH")
        /// };
        /// ]]></example>
        /// </summary>
        protected static readonly Dictionary<Type, KeyValuePair<string, string>[]> ExchangeGlobalCurrencyReplacements = new Dictionary<Type, KeyValuePair<string, string>[]>();

        /// <summary>
        /// 币对符号是否是大写
        /// Whether the symbol is uppercase. Most exchanges are true, but if your exchange is lowercase, set to false in constructor.
        /// </summary>
        public bool MarketSymbolIsUppercase { get; protected set; } = true;

        /// <summary>
        /// Websocket深度信息的类型
        /// 例如：一开始是全集，后面增量、全部是增量、全部是全集 等
        /// The type of web socket order book supported
        /// </summary>
        public WebSocketDepthType WebSocketDepthType { get; protected set; } = WebSocketDepthType.None;
    }
}
