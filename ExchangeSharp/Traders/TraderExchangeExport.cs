using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Centipede
{
    public static class TraderExchangeExport
    {
        /// <summary>
        /// Export exchange data to csv and then to optimized bin files
        /// </summary>
        /// <param name="api">Exchange api, null to just convert existing csv files</param>
        /// <param name="marketSymbol">Market symbol to export</param>
        /// <param name="basePath">Base path to export to, should not contain symbol, symbol will be appended</param>
        /// <param name="sinceDateTime">Start date to begin export at</param>
        /// <param name="callback">Callback if api is not null to notify of progress</param>
        public static void ExportExchangeTrades(IExchangeAPI api, string marketSymbol, string basePath, DateTime sinceDateTime, Action<long> callback = null)
        {
            basePath = Path.Combine(basePath, marketSymbol);
            Directory.CreateDirectory(basePath);
            sinceDateTime = sinceDateTime.ToUniversalTime();
            if (api != null)
            {
                long count = 0;
                int lastYear = -1;
                int lastMonth = -1;
                StreamWriter writer = null;
                bool innerCallback(IEnumerable<ExchangeTrade> trades)
                {
                    foreach (ExchangeTrade trade in trades)
                    {
                        if (trade.Timestamp.Year != lastYear || trade.Timestamp.Month != lastMonth)
                        {
                            if (writer != null)
                            {
                                writer.Close();
                            }
                            lastYear = trade.Timestamp.Year;
                            lastMonth = trade.Timestamp.Month;
                            writer = new StreamWriter(basePath + trade.Timestamp.Year + "-" + trade.Timestamp.Month.ToString("00") + ".csv");
                        }
                        writer.WriteLine("{0},{1},{2}", CryptoUtility.UnixTimestampFromDateTimeSeconds(trade.Timestamp), trade.Price, trade.Amount);
                        if (++count % 100 == 0)
                        {
                            callback?.Invoke(count);
                        }
                    }
                    return true;
                }
                //api.GetHistoricalTradesAsync(innerCallback, marketSymbol, sinceDateTime).Sync();
                writer.Close();
                callback?.Invoke(count);
            }
            TraderFileReader.ConvertCSVFilesToBinFiles(basePath);
        }
    }
}
