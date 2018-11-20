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
}