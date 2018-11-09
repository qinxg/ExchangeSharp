using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExchangeSharp
{
    /// <summary>
    /// Read trades from bin files, convert csv to bin file, etc.
    /// </summary>
    public class TraderFileReader
    {
        public static void ConvertCSVFilesToBinFiles(string folder)
        {
            foreach (string csvFile in Directory.GetFiles(folder, "*.csv", SearchOption.AllDirectories))
            {
                CreateBinFileFromCSVFiles(Path.Combine(Path.GetDirectoryName(csvFile), Path.GetFileNameWithoutExtension(csvFile) + ".bin"), csvFile);
            }
        }

        public static void CreateBinFileFromCSVFiles(string outputFile, params string[] inputFiles)
        {
            unsafe
            {
                Trade trade = new Trade();
                byte[] bytes = new byte[16];
                fixed (byte* ptr = bytes)
                {
                    foreach (string file in inputFiles)
                    {
                        using (StreamReader reader = new StreamReader(file, CryptoUtility.UTF8EncodingNoPrefix))
                        using (Stream writer = File.Create(outputFile))
                        {
                            string line;
                            string[] lines;
                            DateTime dt;
                            while ((line = reader.ReadLine()) != null)
                            {
                                lines = line.Split(',');
                                if (lines.Length == 3)
                                {
                                    dt = CryptoUtility.UnixTimeStampToDateTimeSeconds(double.Parse(lines[0]));
                                    trade.Ticks = (long)CryptoUtility.UnixTimestampFromDateTimeMilliseconds(dt);
                                    trade.Price = float.Parse(lines[1]);
                                    trade.Amount = float.Parse(lines[2]);
                                    if (trade.Amount > 0.01f && trade.Price > 0.5f)
                                    {
                                        *(Trade*)ptr = trade;
                                        writer.Write(bytes, 0, bytes.Length);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static byte[] GetBytesFromBinFiles(string path, DateTime startDate, DateTime endDate)
        {
            string fileName;
            Match m;
            int year, month;
            MemoryStream stream = new MemoryStream();
            byte[] bytes;
            DateTime dt;
            int index;

            unsafe
            {
                Trade* ptrStart, ptrEnd, tradePtr;
                foreach (string binFile in Directory.GetFiles(path, "*.bin", SearchOption.AllDirectories))
                {
                    fileName = Path.GetFileNameWithoutExtension(binFile);
                    m = Regex.Match(fileName, "[0-9][0-9][0-9][0-9]-[0-9][0-9]$");
                    if (m.Success)
                    {
                        year = m.Value.Substring(0, 4).ConvertInvariant<int>();
                        month = m.Value.Substring(5, 2).ConvertInvariant<int>();
                        dt = new DateTime(year, month, startDate.Day, startDate.Hour, startDate.Minute, startDate.Second, startDate.Millisecond, DateTimeKind.Utc);
                        if (dt >= startDate && dt <= endDate)
                        {
                            bytes = File.ReadAllBytes(binFile);
                            fixed (byte* ptr = bytes)
                            {
                                index = 0;
                                ptrStart = (Trade*)ptr;
                                ptrEnd = (Trade*)(ptr + bytes.Length);
                                for (tradePtr = ptrStart; tradePtr != ptrEnd; tradePtr++)
                                {
                                    dt = CryptoUtility.UnixTimeStampToDateTimeMilliseconds(tradePtr->Ticks);
                                    if (dt >= startDate && dt <= endDate)
                                    {
                                        stream.Write(bytes, index, sizeof(Trade));
                                    }
                                    index += sizeof(Trade);
                                    ptrStart++;
                                }
                            }
                        }
                    }
                }
            }

            return stream.ToArray();
        }
    }
}
