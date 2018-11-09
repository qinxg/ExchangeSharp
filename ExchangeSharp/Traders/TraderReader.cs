/*

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeSharp
{
    public interface ITradeReader
    {
        /// <summary>
        /// Read the next trade
        /// </summary>
        /// <param name="trade">Trade to read</param>
        /// <returns>false if no more tickers left, true otherwise</returns>
        bool ReadNextTrade(ref Trade trade);
    }

    /// <summary>
    /// Trader that reads from memory
    /// </summary>
    public sealed unsafe class TradeReaderMemory : ITradeReader, IDisposable
    {
        private byte[] tickerData;
        private Trade* tickers;
        private Trade* tickersStart;
        private Trade* tickersEnd;
        private int tickersCount;
        private GCHandle tickersHandle;
        private bool ownsHandle;

        private TradeReaderMemory() { }

        public TradeReaderMemory(byte[] tickerData)
        {
            this.tickerData = tickerData;
            tickersHandle = GCHandle.Alloc(tickerData, GCHandleType.Pinned);
            tickers = (Trade*)tickersHandle.AddrOfPinnedObject();
            tickersStart = tickers;
            tickersCount = tickerData.Length / 16;
            tickersEnd = tickers + tickersCount;
            ownsHandle = true;
        }

        public void Dispose()
        {
            if (ownsHandle)
            {
                tickersHandle.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadNextTrade(ref Trade ticker)
        {
            if (tickers == tickersEnd)
            {
                ticker.Ticks = 0;
                return false;
            }
            ticker = *tickers++;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ITradeReader Clone()
        {
            return new TradeReaderMemory
            {
                tickerData = tickerData,
                tickers = tickersStart,
                tickersStart = tickersStart,
                tickersEnd = tickersEnd,
                tickersHandle = tickersHandle,
                tickersCount = tickersCount
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            tickers = tickersStart;
        }

        public Trade* TickersPtr { get { return tickers; } }
        public int TickersCount {  get { return tickersCount; } }
    }
}
