

namespace Centipede
{
    /// <summary>
    /// 转账状态
    /// State of transaction status
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>
        /// AwaitingApproval
        /// </summary>
        AwaitingApproval,

        /// <summary>
        /// Complete
        /// </summary>
        Complete,

        /// <summary>
        /// Failure
        /// </summary>
        Failure,

        /// <summary>
        /// Processing
        /// </summary>
        Processing,

        /// <summary>
        /// Rejected
        /// </summary>
        Rejected,

        /// <summary>
        /// Unknown
        /// </summary>
        Unknown
    }
}
