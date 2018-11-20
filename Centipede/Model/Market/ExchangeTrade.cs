using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// 交易数据详情
    /// Details of an exchangetrade
    /// </summary>
    public sealed class ExchangeTrade
    {
        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }


        public Symbol Symbol { get; set; }

        /// <summary>
        /// Trade id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// True if buy, false if sell - for some exchanges (Binance) the meaning can be different, i.e. is the buyer the maker
        /// </summary>
        public bool IsBuy { get; set; }

        /// <summary>
        /// Write to binary writer
        /// </summary>
        /// <param name="writer">Binary writer</param>
        public void ToBinary(BinaryWriter writer)
        {
            writer.Write(DateTime.ToUniversalTime().Ticks);
            writer.Write(Id);
            writer.Write((double)Price);
            writer.Write((double)Amount);
            writer.Write(IsBuy);
        }

        /// <summary>
        /// Read from binary reader
        /// </summary>
        /// <param name="reader">Binary reader</param>
        public void FromBinary(BinaryReader reader)
        {
            DateTime = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);
            Id = reader.ReadString();
            Price = (decimal)reader.ReadDouble();
            Amount = (decimal)reader.ReadDouble();
            IsBuy = reader.ReadBoolean();
        }
    }
}
