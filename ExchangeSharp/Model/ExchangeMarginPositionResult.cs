/*

*/

namespace ExchangeSharp
{
    /// <summary>
    /// Contains information about a margin position on exchange
    /// </summary>
    public class ExchangeMarginPositionResult
    {
        /// <summary>
        /// Market Symbol
        /// </summary>
        public string MarketSymbol { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Total
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Profit (or loss)
        /// </summary>
        public decimal ProfitLoss { get; set; }

        /// <summary>
        /// Fees
        /// </summary>
        public decimal LendingFees { get; set; }

        /// <summary>
        /// Type (exchange dependant)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Base price
        /// </summary>
        public decimal BasePrice { get; set; }

        /// <summary>
        /// Liquidation price
        /// </summary>
        public decimal LiquidationPrice { get; set; }
    }
}
