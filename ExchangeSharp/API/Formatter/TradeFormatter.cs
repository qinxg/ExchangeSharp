namespace Centipede
{
    public class TradeFormatter
    {
        public string AmountKey { get; set; }
        public string PriceKey { get; set; }
        public string DirectionKey { get; set; }
        public string IdKey { get; set; }
        public string DirectionIsBuyValue { get; set; } = "buy";
        public TimestampFormatter TimestampFormatter { get; set; }
    }
}