

namespace Centipede
{
    /// <summary>
    /// 交易所币种信息 
    /// </summary>
    public sealed class Currency
    {
        /// <summary>
        /// 交易所返回的原始名称
        /// </summary>
        public string OriginCurrency { get; set; }

        /// <summary>
        /// 标准名称 （转换后的名称，转换为小写）
        /// </summary>
        public string NormCurrency { get; set; }
    }
}