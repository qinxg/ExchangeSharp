/*

*/

namespace ExchangeSharp
{
    /// <summary> 提币详情
    /// Class to encapsulate details required to make a deposit.</summary>
    public sealed class ExchangeDepositDetails
    {
        /// <summary> 币种名称
        /// The name of the currency. Ex. ETH</summary>
        public string Currency;

        /// <summary> 提币地址
        /// The address to deposit to</summary>
        public string Address;

        /// <summary> 地址备注（标签）
        /// The extra data that must be passed along for currencies like Ripple. Null in
        /// most cases</summary>
        public string AddressTag;

        /// <summary>Returns a <see cref="System.String" /> that represents this instance.</summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return $"{Currency}: Address: {Address} AddressTag: {AddressTag}";
        }
    }
}