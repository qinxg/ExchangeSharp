using System.Security;

namespace ExchangeSharp
{
    /// <summary>
    /// 提币请求
    /// Encapsulation of a withdrawal request from an exchange
    /// </summary>
    public sealed class ExchangeWithdrawalRequest
    {
        /// <summary>The address</summary>
        public string Address { get; set; }

        /// <summary>Gets or sets the address tag.</summary>
        public string AddressTag { get; set; }

        /// <summary>Gets or sets the Amount. Secondary address identifier for coins like XRP,XMR etc</summary>
        public decimal Amount { get; set; }

        /// <summary>Description of the withdrawal</summary>
        public string Description { get; set; }

        /// <summary>Gets or sets the currency to withdraw, i.e. BTC.</summary>
        public string Currency { get; set; }

        /// <summary>
        /// Whether to take the fee from the amount.
        /// Default: true so requests to withdraw an entire balance don't fail.
        /// </summary>
        public bool TakeFeeFromAmount { get; set; } = true;

        /// <summary>
        /// Password if required by the exchange
        /// </summary>
        public SecureString Password { get; set; }

        /// <summary>
        /// Authentication code if required by the exchange
        /// </summary>
        public SecureString Code { get; set; }

        /// <summary>Returns a <see cref="System.String" /> that represents this instance.</summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            // 2.75 ETH to 0x1234asdf
            string info = $"{Amount} {Currency} to {Address}";
            if (!string.IsNullOrWhiteSpace(AddressTag))
            {
                info += $" with address tag {AddressTag}";
            }

            if (!string.IsNullOrWhiteSpace(Description))
            {
                info += $" Description: {Description}";
            }

            return info;
        }
    }
}