

namespace Centipede.Binance
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    internal class MarketDepthDiffUpdate
    {
        [JsonProperty("e")]
        public string EventType { get; set; }

        [JsonProperty("E")]
        public long EventTime { get; set; }

        [JsonProperty("s")]
        public string MarketSymbol { get; set; }

        [JsonProperty("U")]
        public int FirstUpdate { get; set; }

        [JsonProperty("u")]
        public int FinalUpdate { get; set; }

        [JsonProperty("b")]
        public List<List<object>> Bids { get; set; }

        [JsonProperty("a")]
        public List<List<object>> Asks { get; set; }
    }
}