

namespace Centipede
{
    /// <summary>
    /// 币种信息 
    /// </summary>
    public sealed class ExchangeCurrency
    {
        /// <summary>Short name of the currency. Eg. ETH</summary>
        public string Name { get; set; }

        /// <summary>Full name of the currency. Eg. Ethereum</summary>
        public string FullName { get; set; }

        /// <summary>The transaction fee.</summary>
        public decimal TxFee { get; set; }

        /// <summary>A value indicating whether deposit is enabled.</summary>
        public bool DepositEnabled { get; set; }

        /// <summary>A value indicating whether withdrawal is enabled.</summary>
        public bool WithdrawalEnabled { get; set; }

        /// <summary>Extra information from the exchange.</summary>
        public string Notes { get; set; }

        /// <summary>
        /// The type of the coin.
        /// Examples from Bittrex: BITCOIN, NXT, BITCOINEX, NXT_MS,
        /// CRYPTO_NOTE_PAYMENTID, BITSHAREX, COUNTERPARTY, BITCOIN_STEALTH, RIPPLE, ETH_CONTRACT,
        /// NEM, ETH, OMNI, LUMEN, FACTOM, STEEM, BITCOIN_PERCENTAGE_FEE, LISK, WAVES, ANTSHARES,
        /// WAVES_ASSET, BYTEBALL, SIA, ADA
        /// </summary>
        public string CoinType { get; set; }

        /// <summary>The base address where a two-field coin is sent. For example, Ripple deposits
        /// are all sent to this address with a tag field</summary>
        public string BaseAddress { get; set; }

        /// <summary>Minimum number of confirmations before deposit will post at the exchange</summary>
        public int MinConfirmations { get; set; }
    }
}