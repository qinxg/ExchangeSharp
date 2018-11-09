﻿namespace ExchangeSharp
{
    /// <summary>
    /// 行情集合接口
    /// </summary>
    public interface IMarketProvider : ITickerProvider, ITradeProvider, IKlineProvider, IOrderBookProvider
    {

    }
}
