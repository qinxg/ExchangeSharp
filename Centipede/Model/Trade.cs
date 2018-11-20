using System;
using System.Runtime.InteropServices;

namespace Centipede
{
    /// <summary>
    /// 实时交易情况
    /// A tight, lightweight trade object useful for iterating quickly in memory for trader testing
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Trade
    {
        /// <summary>
        /// Unix timestamp in milliseconds, or 0 if no new trade available
        /// </summary>
        public long Ticks;

        /// <summary>
        /// Current purchase price
        /// </summary>
        public float Price;

        /// <summary>
        /// Amount purchased
        /// </summary>
        public float Amount;

        /// <summary>
        /// Get a string for this trade
        /// </summary>
        /// <returns>String</returns>
        public override string ToString()
        {
            return CryptoUtility.UnixTimeStampToDateTimeMilliseconds(Ticks).ToLocalTime() + ": " + Amount + " at " + Price;
        }
    }
}
