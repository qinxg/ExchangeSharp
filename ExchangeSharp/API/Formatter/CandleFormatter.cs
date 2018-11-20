namespace Centipede
{
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
}