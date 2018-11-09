using System;
using System.Collections.Generic;
using ExchangeSharp;

namespace ExchangeSharpConsole
{
	public static partial class ExchangeSharpConsoleMain
    {
        public static void RunGetHistoricalTrades(Dictionary<string, string> dict)
        {
            RequireArgs(dict, "exchangeName", "marketSymbol");

            string exchangeName = dict["exchangeName"];
            IExchangeAPI api = ExchangeAPI.GetExchangeAPI(exchangeName);
            string marketSymbol = dict["marketSymbol"];
            Console.WriteLine("Showing historical trades for exchange {0}...", exchangeName);
            DateTime? startDate = null;
            DateTime? endDate = null;
            if (dict.ContainsKey("startDate"))
            {
                startDate = DateTime.Parse(dict["startDate"]).ToUniversalTime();
            }
            if (dict.ContainsKey("endDate"))
            {
                endDate = DateTime.Parse(dict["endDate"]).ToUniversalTime();
            }
            api.GetHistoricalTradesAsync((IEnumerable<ExchangeTrade> trades) =>
            {
                foreach (ExchangeTrade trade in trades)
                {
                    Console.WriteLine("Trade at timestamp {0}: {1}/{2}/{3}", trade.Timestamp.ToLocalTime(), trade.Id, trade.Price, trade.Amount);
                }
                return true;
            }, marketSymbol, startDate, endDate).Sync();
        }

        public static void RunExportData(Dictionary<string, string> dict)
        {
            RequireArgs(dict, "exchange", "symbol", "path", "sinceDateTime");
            string exchange = dict["exchange"];
            long total = 0;
            TraderExchangeExport.ExportExchangeTrades(ExchangeAPI.GetExchangeAPI(exchange), dict["symbol"], dict["path"], DateTime.Parse(dict["sinceDateTime"]), (long count) =>
            {
                total = count;
                Console.Write("Exporting {0}: {1}     \r", exchange, total);
            });
            Console.WriteLine("{0}Finished Exporting {1}: {2}     \r", Environment.NewLine, exchange, total);
        }
    }
}
