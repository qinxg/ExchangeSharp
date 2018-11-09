using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;using Newtonsoft.Json;

namespace ExchangeSharp
{
    /// <summary>
    /// 行情数据
    /// Details of the current price of an exchange asset
    /// </summary>
    public sealed class ExchangeTicker
    {
        /// <summary>
        /// An exchange specific id if known, otherwise null
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 当前交易所的对应交易对
        /// The currency pair symbol that this ticker is in reference to
        /// </summary>
        public string MarketSymbol { get; set; }

        /// <summary>
        /// The bid is the price to sell at
        /// </summary>
        public decimal Bid { get; set; }

        /// <summary>
        /// The ask is the price to buy at
        /// </summary>
        public decimal Ask { get; set; }

        /// <summary>
        /// The last trade purchase price
        /// </summary>
        public decimal Last { get; set; }

        /// <summary>
        /// Volume info
        /// </summary>
        public ExchangeVolume Volume { get; set; }

        /// <summary>
        /// Get a string for this ticker
        /// </summary>
        /// <returns>String</returns>
        public override string ToString()
        {
            return string.Format("Bid: {0}, Ask: {1}, Last: {2}", Bid, Ask, Last);
        }

        /// <summary>
        /// Write to writer
        /// </summary>
        /// <param name="writer">Writer</param>
        public void ToBinary(BinaryWriter writer)
        {
            writer.Write((double)Bid);
            writer.Write((double)Ask);
            writer.Write((double)Last);
            Volume.ToBinary(writer);
        }

        /// <summary>
        /// Read from reader
        /// </summary>
        /// <param name="reader">Reader</param>
        public void FromBinary(BinaryReader reader)
        {
            Bid = (decimal)reader.ReadDouble();
            Ask = (decimal)reader.ReadDouble();
            Last = (decimal)reader.ReadDouble();
            Volume = (Volume ?? new ExchangeVolume());
            Volume.FromBinary(reader);
        }
    }

    /// <summary>
    /// Info about exchange volume
    /// </summary>
    public sealed class ExchangeVolume
    {
        /// <summary>
        /// Last volume update timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Quote / Price currency - will equal base currency if exchange doesn't break it out by price unit and quantity unit
        /// In BTC-USD, this would be USD
        /// </summary>
        public string QuoteCurrency { get; set; }

        /// <summary>
        /// Amount in units of the QuoteCurrency - will equal BaseCurrencyVolume if exchange doesn't break it out by price unit and quantity unit
        /// In BTC-USD, this would be USD volume
        /// </summary>
        public decimal QuoteCurrencyVolume { get; set; }

        /// <summary>
        /// Base currency
        /// In BTC-USD, this would be BTC
        /// </summary>
        public string BaseCurrency { get; set; }

        /// <summary>
        /// Base currency amount (this many units total)
        /// In BTC-USD this would be BTC volume
        /// </summary>
        public decimal BaseCurrencyVolume { get; set; }

        /// <summary>
        /// Write to a binary writer
        /// </summary>
        /// <param name="writer">Binary writer</param>
        public void ToBinary(BinaryWriter writer)
        {
            writer.Write(Timestamp.ToUniversalTime().Ticks);
            writer.Write(QuoteCurrency);
            writer.Write((double)QuoteCurrencyVolume);
            writer.Write(BaseCurrency);
            writer.Write((double)BaseCurrencyVolume);
        }

        /// <summary>
        /// Read from a binary reader
        /// </summary>
        /// <param name="reader">Binary reader</param>
        public void FromBinary(BinaryReader reader)
        {
            Timestamp = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);
            QuoteCurrency = reader.ReadString();
            QuoteCurrencyVolume = (decimal)reader.ReadDouble();
            BaseCurrency = reader.ReadString();
            BaseCurrencyVolume = (decimal)reader.ReadDouble();
        }
    }
}
