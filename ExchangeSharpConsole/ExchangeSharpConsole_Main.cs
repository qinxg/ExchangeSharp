using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;using Centipede;

namespace CentipedeConsole
{
    public static partial class CentipedeConsoleMain
    {
        public static int Main(string[] args)
        {

            var huobi = ExchangeAPI.GetExchangeAPI("Huobi");
            var ok = ExchangeAPI.GetExchangeAPI("Okex");

            var okresult = ok.GetMarketSymbolsMetadataAsync().Result;
            var hbresult = huobi.GetMarketSymbolsMetadataAsync().Result;

            Console.WriteLine(hbresult);
            Console.WriteLine(okresult);

            return CentipedeConsoleMain.ConsoleMain(args);
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

