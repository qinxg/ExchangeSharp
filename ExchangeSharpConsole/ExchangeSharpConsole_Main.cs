using Centipede;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CentipedeConsole
{
    public static partial class CentipedeConsoleMain
    {
        public static int Main(string[] args)
        {

            var huobi = ExchangeAPI.GetExchangeAPI("Huobi");
            var currencies = huobi.GetCurrenciesAsync().Result;
            Console.WriteLine(currencies);

            huobi.LoadInformation(currencies, null);

            var symbls = huobi.GetSymbolsAsync().Result;
            Console.WriteLine(symbls);

            huobi.LoadInformation(currencies, symbls);

            ////ticker
            //var tickers = huobi.GetTickersAsync().Result;
            //Console.WriteLine(tickers);

            var symbol = symbls.Get("eth", "usdt");
            //var ticker = huobi.GetTickerAsync(symbol).Result;
            //Console.WriteLine(ticker);


            ////trade
            //var trades = huobi.GetTradesAsync(symbol).Result;
            //Console.WriteLine(trades);


            ////K线
            //var kline = huobi.GetCandlesAsync(symbol, 60).Result;
            //Console.WriteLine(kline);

            ////depth
            //var depth = huobi.GetDepthAsync(symbol, 5).Result;
            //Console.WriteLine(depth);



            //ws
            Task.Run(() =>
            {
                huobi.GetDepthWebSocket(d =>
                {
                    //var json = JsonConvert.SerializeObject(d);
                    //Console.WriteLine(json);
                }, 5, symbol);
            });

            Task.Run(() =>
            {
                huobi.GetTickerWebSocket(d =>
                    {
                        var json = JsonConvert.SerializeObject(d);
                        Console.WriteLine(json);
                    }, symbol
                );
            });


            //huobi.LoadAPIKeysUnsecure("f47cffdb-682ae30b-56b1c197-8a75c", "690fd255-e3d1b533-bec93d39-8b851");

            //var order = huobi.PlaceOrderAsync(new ExchangeOrderRequest
            //{
            //    Amount = 0.001m,
            //    Price = 0.001m,
            //    IsBuy = true,
            //    IsMargin = false,
            //    OrderType = OrderType.Limit,
            //    Symbol = symbol
            //}).Result;

            //Console.WriteLine(order);

            //huobi.CancelOrderAsync(order.OrderId).Wait();

            //var detail = huobi.GetOrderDetailsAsync(order.OrderId).Result;

            Console.ReadKey();
            //return CentipedeConsoleMain.ConsoleMain(args);

            return 0;
        }


        private static void RequireArgs(Dictionary<string, string> dict, params string[] args)
        {
            bool fail = false;
            foreach (string arg in args)
            {
                if (!dict.ContainsKey(arg))
                {
                    Logger.Error("Argument {0} is required.", arg);
                    fail = true;
                }
            }
            if (fail)
            {
                throw new ArgumentException("Missing required arguments");
            }
        }

        private static Dictionary<string, string> ParseCommandLine(string[] args)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string a in args)
            {
                int idx = a.IndexOf('=');
                string key = (idx < 0 ? a.Trim('-') : a.Substring(0, idx)).ToLowerInvariant();
                string value = (idx < 0 ? string.Empty : a.Substring(idx + 1));
                dict[key] = value;
            }
            return dict;
        }

        private static void TestMethod()
        {
        }

        public static int ConsoleMain(string[] args)
        {
            try
            {
                Logger.Info("Centipede console started.");

                // swap out to external web socket implementation for older Windows pre 8.1
                // Centipede.ClientWebSocket.RegisterWebSocketCreator(() => new CentipedeConsole.WebSocket4NetClientWebSocket());
                // TestMethod(); return 0; // uncomment for ad-hoc code testing

                Dictionary<string, string> argsDictionary = ParseCommandLine(args);
                if (argsDictionary.Count == 0 || argsDictionary.ContainsKey("help"))
                {
                    RunShowHelp(argsDictionary);
                }
                
                else if (argsDictionary.Count >= 1 && argsDictionary.ContainsKey("export"))
                {
                    RunExportData(argsDictionary);
                }
                else if (argsDictionary.Count >= 1 && argsDictionary.ContainsKey("convert"))
                {
                    RunConvertData(argsDictionary);
                }
                else if (argsDictionary.ContainsKey("showHistoricalTrades"))
                {
                    RunGetHistoricalTrades(argsDictionary);
                }
                else if (argsDictionary.ContainsKey("getOrderHistory"))
                {
                    RunGetOrderHistory(argsDictionary);
                }
                else if (argsDictionary.ContainsKey("getOrderDetails"))
                {
                    RunGetOrderDetails(argsDictionary);
                }
                else
                {
                    Logger.Error("Unrecognized command line arguments.");
                    return -1;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return -99;
            }
            finally
            {
                Logger.Info("Centipede console finished.");
            }
        }
    }
}

