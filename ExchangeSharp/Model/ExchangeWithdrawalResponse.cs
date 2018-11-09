

namespace Centipede
{
    /// <summary>
    /// 提币结果
    /// Class encapsulating a withdrawal response from an exchange
    /// </summary>
    public sealed class ExchangeWithdrawalResponse
    {
        /// <summary>The message of the transacion.</summary>
        public string Message { get; set; }

        /// <summary>The identifier for the transaction.</summary>
        public string Id { get; set; }

        /// <summary>Whether the withdrawal was successful</summary>
        public bool Success { get; set; } = true;

        /// <summary>Returns a <see cref="System.String" /> that represents this instance.</summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return $"Success: {Success} Id: {Id ?? "null"} Message: {Message ?? "null"}";
        }
    }
}