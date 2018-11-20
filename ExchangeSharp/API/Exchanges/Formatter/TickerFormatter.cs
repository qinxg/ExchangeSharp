namespace Centipede
{

    public class TickerFormatter
    {
        public string LastKey { get; set; }

        public string IdKey { get; set; }

        public TimestampFormatter TimestampFormatter { get; set; }
        public VolumeFormatter VolumeFormatter { get; set; }

        public AskBidFormatter AskBidFormatter { get; set; }
    }


    public class CandleFormatter
    {
        public int PeriodSeconds { get; set; }


        public string OpenKey { get; set; }
        public string HighKey { get; set; }
        public string LowKey { get; set; }
        public string CloseKey { get; set; }

        public TimestampFormatter TimestampFormatter { get; set; }
        public VolumeFormatter VolumeFormatter { get; set; }

        public string WeightedAverageKey { get; set; }
    }



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