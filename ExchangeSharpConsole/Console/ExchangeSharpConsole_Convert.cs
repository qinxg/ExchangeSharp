/*

*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ExchangeSharp;

namespace ExchangeSharpConsole
{
    public static partial class ExchangeSharpConsoleMain
    {
        public static void RunConvertData(Dictionary<string, string> dict)
        {
            RequireArgs(dict, "symbol", "path");
            TraderExchangeExport.ExportExchangeTrades(null, dict["symbol"], dict["path"], CryptoUtility.UtcNow);
        }
    }
}
