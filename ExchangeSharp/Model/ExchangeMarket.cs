﻿

namespace ExchangeSharp
{
    /// <summary> 代表交易所的一个币对
    /// Representation of a market on an exchange.</summary>
    public sealed class ExchangeMarket
    {
        /// <summary>Id of the market (specific to the exchange), null if none</summary>
        public string MarketId { get; set; }

        /// <summary>币对编码
        /// Gets or sets the symbol representing the market's currency pair.</summary>
        public string MarketSymbol { get; set; }

        /// <summary>A value indicating whether the market is active.</summary>
        public bool IsActive { get; set; }

        /// <summary> 计价币种	
        /// In a pair like ZRX/BTC, BTC is the quote currency.</summary>
        public string QuoteCurrency { get; set; }

        /// <summary> 基础币种	
        /// In a pair like ZRX/BTC, ZRX is the base currency.</summary>
        public string BaseCurrency { get; set; }

        /// <summary> 最小交易规模
        /// The minimum size of the trade in the unit of "BaseCurrency". For example, in
        /// DOGE/BTC the MinTradeSize is currently 423.72881356 DOGE</summary>
        public decimal MinTradeSize { get; set; }

        /// <summary>The maximum size of the trade in the unit of "BaseCurrency".</summary>
        public decimal MaxTradeSize { get; set; } = decimal.MaxValue;

        /// <summary> 以“计价货币”为单位的最低交易规模
        /// The minimum size of the trade in the unit of "QuoteCurrency". To determine an order's
        /// trade size in terms of the Quote Currency, you need to calculate: price * quantity
        /// NOTE: Not all exchanges provide this information</summary>
        public decimal? MinTradeSizeInQuoteCurrency { get; set; }

        /// <summary>The maximum size of the trade in the unit of "QuoteCurrency". To determine an order's
        /// trade size in terms of the Quote Currency, you need to calculate: price * quantity
        /// NOTE: Not all exchanges provide this information</summary>
        public decimal? MaxTradeSizeInQuoteCurrency { get; set; }

        /// <summary> 最小价格
        /// The minimum price of the pair.</summary>
        public decimal MinPrice { get; set; }

        /// <summary> 最大价格
        /// The maximum price of the pair.</summary>
        public decimal MaxPrice { get; set; } = decimal.MaxValue;

        /// <summary> 价格步长
        /// Defines the intervals that a price can be increased/decreased by. The following
        /// must be true for price: Price % PriceStepSize == 0 Null if unknown or not applicable.</summary>
        public decimal? PriceStepSize { get; set; }

        /// <summary> 数量步长
        /// Defines the intervals that a quantity can be increased/decreased by. The
        /// following must be true for quantity: (Quantity-MinTradeSize) % QuantityStepSize == 0 Null
        /// if unknown or not applicable.</summary>
        public decimal? QuantityStepSize { get; set; }

        /// <summary>
        /// 是否支持杠杆
        /// Margin trading enabled for this market
        /// </summary>
        public bool MarginEnabled { get; set; }

        public override string ToString()
        {
            return $"{MarketSymbol}, {BaseCurrency}-{QuoteCurrency}";
        }
    }
}