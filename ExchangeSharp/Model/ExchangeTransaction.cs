

namespace Centipede
{
    using System;

    /// <summary>交易所提币或充币信息
    /// An encapsulation of a deposit or withdrawal to an exchange</summary>
    public sealed class ExchangeTransaction
    {
        /// <summary>The address the transaction was sent to</summary>
        public string Address { get; set; }

        /// <summary>The address tag used for currencies like Ripple</summary>
        public string AddressTag { get; set; }

        /// <summary>The amount of the transaction</summary>
        public decimal Amount { get; set; }

        /// <summary>The transaction identifier on the blockchain</summary>
        public string BlockchainTxId { get; set; }

        /// <summary>Open text field to track notes</summary>
        public string Notes { get; set; }

        /// <summary>The payment identifier on the site</summary>
        public string PaymentId { get; set; }

        /// <summary>A value indicating whether the transaction is pending</summary>
        public TransactionStatus Status { get; set; } = TransactionStatus.Unknown;

        /// <summary>The timestamp of the transaction, should be in UTC</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>The fee on the transaction</summary>
        public decimal TxFee { get; set; }

        /// <summary>The currency name (ex. BTC)</summary>
        public string Currency { get; set; }

        public override string ToString()
        {
            return
                $"{Amount} {Currency} (fee: {TxFee}) sent to Address: {Address ?? "null"} with AddressTag: {AddressTag ?? "null"} BlockchainTxId: {BlockchainTxId ?? "null"} sent at {Timestamp} UTC. Status: {Status}. Exchange paymentId: {PaymentId ?? "null"}. Notes: {Notes ?? "null"}";
        }
    }
}