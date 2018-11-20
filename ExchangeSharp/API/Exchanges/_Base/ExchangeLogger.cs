using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// Logs data from an exchange
    /// </summary>
    public sealed class ExchangeLogger : IDisposable
    {
        private readonly AutoResetEvent cancelEvent = new AutoResetEvent(false);

        private BinaryWriter sysTimeWriter;
        private BinaryWriter tickerWriter;
        private BinaryWriter bookWriter;
        private BinaryWriter tradeWriter;

        HashSet<string> tradeIds = new HashSet<string>();
        HashSet<string> tradeIds2 = new HashSet<string>();

        private void LoggerThread()
        {
            while (IsRunningInBackground && !cancelEvent.WaitOne(Interval))
            {
                Update();
            }
            cancelEvent.Set();
            IsRunningInBackground = false;
        }

        private BinaryWriter CreateLogWriter(string path, bool compress)
        {
            Stream stream = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            if (compress)
            {
                stream = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionLevel.Optimal, false);
            }
            return new BinaryWriter(stream);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="api">API</param>
        /// <param name="marketSymbol">The symbol to log, i.e. btcusd</param>
        /// <param name="intervalSeconds">Interval in seconds between updates</param>
        /// <param name="path">The path to write the log files to</param>
        /// <param name="compress">Whether to compress the log files using gzip compression</param>
        public ExchangeLogger(IExchangeAPI api, string marketSymbol, float intervalSeconds, string path, bool compress = false)
        {
            string compressExtension = (compress ? ".gz" : string.Empty);
            API = api;
            MarketSymbol = marketSymbol;
            Interval = TimeSpan.FromSeconds(intervalSeconds);
            sysTimeWriter = CreateLogWriter(Path.Combine(path, api.Name + "_time.bin" + compressExtension), compress);
            tickerWriter = CreateLogWriter(Path.Combine(path, api.Name + "_ticker.bin" + compressExtension), compress);
            bookWriter = CreateLogWriter(Path.Combine(path, api.Name + "_book.bin" + compressExtension), compress);
            tradeWriter = CreateLogWriter(Path.Combine(path, api.Name + "_trades.bin" + compressExtension), compress);
        }

        /// <summary>
        /// Update the logger - you can call this periodically if you don't want to call Start to run the logger in a background thread.
        /// </summary>
        public void Update()
        {
            ExchangeTrade[] newTrades;
            HashSet<string> tmpTradeIds;

            try
            {
                if (MarketSymbol == "*")
                {
                    // get all symbols
                    Tickers = API.GetTickersAsync().Sync();
                    tickerWriter.Write(Tickers.Count);
                    foreach ( ExchangeTicker ticker in Tickers)
                    {
                        //tickerWriter.Write(ticker.Key);
                        //todo:
                        //ticker.Value.ToBinary(tickerWriter);
                    }
                }
                else
                {
                    // make API calls first, if they fail we will try again later
                    //todo  Tickers = new KeyValuePair<string, ExchangeTicker>[1] { new KeyValuePair<string, ExchangeTicker>(Symbol, API.GetTickerAsync(Symbol).Sync()) };
                    //Depth = API.GetDepthAsync(Symbol).Sync();
                    //Trades = API.GetTradesAsync(Symbol).Sync().OrderBy(t => t.DateTime).ToArray();

                    // all API calls succeeded, we can write to files

                    // write system date / time
                    sysTimeWriter.Write(CryptoUtility.UtcNow.Ticks);

                    // write ticker
                    //TODO:
                    //Tickers.First().Value.ToBinary(tickerWriter);

                    // write order book
                    Depth.ToBinary(bookWriter);

                    // new trades only
                    newTrades = Trades.Where(t => !tradeIds.Contains(t.Id)).ToArray();

                    // write new trades
                    tradeWriter.Write(newTrades.Length);
                    foreach (ExchangeTrade trade in newTrades)
                    {
                        trade.ToBinary(tradeWriter);
                    }

                    // track trade ids for the latest set of trades
                    foreach (ExchangeTrade trade in Trades)
                    {
                        tradeIds2.Add(trade.Id);
                    }
                    tmpTradeIds = tradeIds;
                    tradeIds = tradeIds2;
                    tradeIds2 = tmpTradeIds;
                    tradeIds2.Clear();
                }

                DataAvailable?.Invoke(this);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        /// <summary>
        /// Start a background thread for the logger
        /// </summary>
        /// <returns>True if started, false if already running in which case nothing happens</returns>
        public bool Start()
        {
            if (!IsRunningInBackground)
            {
                IsRunningInBackground = true;
                Task.Factory.StartNew(LoggerThread);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stop the logger background thread if it is running and waits for completion. Does not close the logger files. The logger can be started again later.
        /// </summary>
        public void Stop()
        {
            if (IsRunningInBackground)
            {
                cancelEvent.Set();
                cancelEvent.WaitOne();
            }
        }

        /// <summary>
        /// Close the logger
        /// </summary>
        public void Dispose()
        {
            Stop();
            sysTimeWriter.Close();
            tickerWriter.Close();
            bookWriter.Close();
            tradeWriter.Close();
        }

        /// <summary>
        /// Open a log reader for a base path - will detect if there is a compressed version automatically
        /// </summary>
        /// <param name="basePath">Base path (i.e. logFile.bin)</param>
        /// <returns>BinaryReader</returns>
        public static BinaryReader OpenLogReader(string basePath)
        {
            if (File.Exists(basePath))
            {
                return new BinaryReader(File.OpenRead(basePath));
            }
            return new BinaryReader(new System.IO.Compression.GZipStream(File.OpenRead(basePath + ".gz"), System.IO.Compression.CompressionMode.Decompress, false));
        }

        /// <summary>
        /// Begins logging exchanges - writes errors to console. You should block the app using Console.ReadLine.
        /// </summary>
        /// <param name="path">Path to write files to</param>
        /// <param name="intervalSeconds">Interval in seconds in between each log calls for each exchange</param>
        /// <param name="terminateAction">Call this when the process is about to exit, like a WM_CLOSE message on Windows.</param>
        /// <param name="compress">Whether to compress the log files</param>
        /// <param name="exchangeNamesAndSymbols">Exchange names and symbols to log</param>
        public static void LogExchanges(string path, float intervalSeconds, out Action terminateAction, bool compress, params string[] exchangeNamesAndSymbols)
        {
            bool terminating = false;
            Action terminator = null;
            path = (string.IsNullOrWhiteSpace(path) ? "./" : path);
            Dictionary<ExchangeLogger, int> errors = new Dictionary<ExchangeLogger, int>();
            List<ExchangeLogger> loggers = new List<ExchangeLogger>();
            for (int i = 0; i < exchangeNamesAndSymbols.Length;)
            {
                loggers.Add(new ExchangeLogger(ExchangeAPI.GetExchangeAPI(exchangeNamesAndSymbols[i++]), exchangeNamesAndSymbols[i++], intervalSeconds, path, compress));
            };
            foreach (ExchangeLogger logger in loggers)
            {
                logger.Start();
                logger.Error += (log, ex) =>
                {
                    int errorCount;
                    lock (errors)
                    {
                        if (!errors.TryGetValue(log, out errorCount))
                        {
                            errorCount = 0;
                        }
                        errors[log] = ++errorCount;
                    }
                    Logger.Info("Errors for {0}: {1}", log.API.Name, errorCount);
                };
            }
            terminator = () =>
            {
                if (!terminating)
                {
                    terminating = true;
                    foreach (ExchangeLogger logger in loggers.ToArray())
                    {
                        logger.Stop();
                        logger.Dispose();
                    }
                    loggers.Clear();
                }
            };
            terminateAction = terminator;

            // make sure to close properly
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                terminator();
            };
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) =>
            {
                terminator();
            };
            Logger.Info("Loggers \"{0}\" started, press ENTER or CTRL-C to terminate.", string.Join(", ", loggers.Select(l => l.API.Name)));
        }

        /// <summary>
        /// Enumerate over a log file that contains multiple tickers per entry. The previous dictionary is not valid once the enumerator is moved.
        /// Multi ticker log format: [int32 count](count times:)[string key][exchange ticker]
        /// </summary>
        /// <param name="path">Path to read from</param>
        /// <returns>Enumerator returning the tickers for each entry</returns>
        public static IEnumerable<Dictionary<string, ExchangeTicker>> ReadMultiTickers(string path)
        {
            int count;
            Dictionary<string, ExchangeTicker> tickers = new Dictionary<string, ExchangeTicker>(StringComparer.OrdinalIgnoreCase);
            ExchangeTicker ticker;
            string key;
            using (BinaryReader tickerReader = ExchangeLogger.OpenLogReader(path))
            {
                while (true)
                {
                    try
                    {
                        tickers.Clear();
                        count = tickerReader.ReadInt32();
                        while (count-- > 0)
                        {
                            key = tickerReader.ReadString();
                            ticker = new ExchangeTicker();
                            //todo: ticker.FromBinary(tickerReader);
                            tickers[key] = ticker;
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }
                    yield return tickers;
                }
            }
        }

        /// <summary>
        /// The exchange API being logged
        /// </summary>
        public IExchangeAPI API { get; private set; }

        /// <summary>
        /// The symbol being logged
        /// </summary>
        public string MarketSymbol { get; private set; }

        /// <summary>
        /// The interval in between log calls
        /// </summary>
        public TimeSpan Interval { get; private set; }

        /// <summary>
        /// Whether the logger is running
        /// </summary>
        public bool IsRunningInBackground { get; set; }

        /// <summary>
        /// Event that fires when there is an error
        /// </summary>
        public event Action<ExchangeLogger, Exception> Error;

        /// <summary>
        /// Event that fires when new log data is available
        /// </summary>
        public event Action<ExchangeLogger> DataAvailable;

        /// <summary>
        /// Latest tickers
        /// </summary>
        public IReadOnlyCollection< ExchangeTicker> Tickers { get; private set; }

        /// <summary>
        /// Latest order book
        /// </summary>
        public ExchangeDepth Depth { get; private set; }

        /// <summary>
        /// Latest trades
        /// </summary>
        public ExchangeTrade[] Trades { get; private set; }
    }
}

