

namespace ExchangeSharp.Binance
{
    using Newtonsoft.Json;

    internal class MultiDepthStream
    {
        [JsonProperty("stream")]
        public string Stream { get; set; }

        [JsonProperty("data")]
        public MarketDepthDiffUpdate Data { get; set; }
    }
}