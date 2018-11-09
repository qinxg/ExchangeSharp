﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExchangeSharp
{
    /// <summary>
    /// 基础信息
    /// </summary>
    public interface ICommonProvider
    {
        /// <summary>
        /// 获取可用币种信息
        /// </summary>
        /// <returns>Collection of Currencies</returns>
        Task<IReadOnlyDictionary<string, ExchangeCurrency>> GetCurrenciesAsync();


        /// <summary>
        /// 获取当前交易所所有币对，以及相关限制
        /// Get exchange market symbols including available metadata such as min trade size and whether the market is active
        /// </summary>
        /// <returns>Collection of ExchangeMarkets</returns>
        Task<IEnumerable<ExchangeMarket>> GetMarketSymbolsMetadataAsync();

    }
}