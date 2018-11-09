using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeSharp
{
    public class SimplePeakValleyTrader : Trader
    {
        public double AnchorPrice { get; private set; }
        public bool HitValley { get; private set; }
        public bool HitPeak { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void Initialize(ExchangeTradeInfo tradeInfo)
        {
            base.Initialize(tradeInfo);

            SetPlotListCount(1);
            AnchorPrice = TradeInfo.Trade.Price;
            HitValley = HitPeak = false;
            ProcessTrade();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void ProcessTrade()
        {
            double diff = TradeInfo.Trade.Price - AnchorPrice;
            PlotPoints[0].Add(new KeyValuePair<float, float>(TradeInfo.Trade.Ticks, TradeInfo.Trade.Price));
            if (HitValley && diff <= ((BuyThresholdPercent * AnchorPrice) + (BuyReverseThresholdPercent * AnchorPrice)))
            {
                // valley reversal, buy
                // lower anchor price just a bit in case price drops so we will buy more
                AnchorPrice -= (BuyFalseReverseThresholdPercent * AnchorPrice);
                HitPeak = false;
                PerformBuy();
            }
            else if (HitPeak && diff >= ((SellThresholdPercent * AnchorPrice) + (SellReverseThresholdPercent * AnchorPrice)) &&
                BuyPrices.Count != 0 && TradeInfo.Trade.Price > BuyPrices[BuyPrices.Count - 1].Value + (SellReverseThresholdPercent * AnchorPrice))
            {
                // peak reversal, sell
                AnchorPrice = TradeInfo.Trade.Price;
                HitPeak = HitValley = false;
                PerformSell();
            }
            else if (diff < (BuyThresholdPercent * AnchorPrice))
            {
                // valley
                HitValley = true;
                HitPeak = false;
                AnchorPrice = TradeInfo.Trade.Price;
            }
            else if (diff > (SellThresholdPercent * AnchorPrice))
            {
                // peak
                HitPeak = true;
                HitValley = false;
                AnchorPrice = TradeInfo.Trade.Price;
            }
            else
            {
                // watch
            }
        }
    }
}
