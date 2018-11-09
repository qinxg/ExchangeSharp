using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Centipede
{
    /// <summary>
    /// Base interface for all API implementations
    /// </summary>
    public interface IBaseAPI : INamed
    {
        #region Properties

        /// <summary>
        /// Optional public API key
        /// </summary>
        SecureString PublicApiKey { get; set; }

        /// <summary>
        /// Optional private API key
        /// </summary>
        SecureString PrivateApiKey { get; set; }

        /// <summary>
        /// Pass phrase API key - only needs to be set if you are using private authenticated end points. Please use CryptoUtility.SaveUnprotectedStringsToFile to store your API keys, never store them in plain text!
        /// Most exchanges do not require this, but Coinbase is an example of one that does
        /// </summary>
        System.Security.SecureString Passphrase { get; set; }

        /// <summary>
        /// Request timeout
        /// </summary>
        TimeSpan RequestTimeout { get; set; }

        /// <summary>
        /// Request window - most services do not use this, but Binance API is an example of one that does
        /// </summary>
        TimeSpan RequestWindow { get; set; }

        /// <summary>
        /// Nonce style
        /// </summary>
        NonceStyle NonceStyle { get; }

        /// <summary>
        /// Cache policy - defaults to no cache, don't change unless you have specific needs
        /// </summary>
        System.Net.Cache.RequestCachePolicy RequestCachePolicy { get; set; }

        /// <summary>
        /// Cache policy for api methods (method name, cache time)
        /// </summary>
        Dictionary<string, TimeSpan> MethodCachePolicy { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Load API keys from an encrypted file - keys will stay encrypted in memory
        /// </summary>
        /// <param name="encryptedFile">Encrypted file to load keys from</param>
        void LoadAPIKeys(string encryptedFile);

        /// <summary>
        ///  Load API keys from unsecure strings
        /// <param name="publicApiKey">Public Api Key</param>
        /// <param name="privateApiKey">Private Api Key</param>
        /// <param name="passPhrase">Pass phrase, null for none</param>
        /// </summary>
        void LoadAPIKeysUnsecure(string publicApiKey, string privateApiKey, string passPhrase = null);

        /// <summary>
        /// 产生随机数 见：https://www.cnblogs.com/bestzrz/archive/2011/09/03/2164620.html
        /// </summary>
        /// <returns>Nonce (can be string, long, double, etc., so object is used)</returns>
        Task<object> GenerateNonceAsync();

        /// <summary>
        /// Make a JSON request to an API end point
        /// </summary>
        /// <typeparam name="T">Type of object to parse JSON as</typeparam>
        /// <param name="url">Path and query</param>
        /// <param name="baseUrl">Override the base url, null for the default BaseUrl</param>
        /// <param name="payload">Payload, can be null. For private API end points, the payload must contain a 'nonce' key set to GenerateNonce value.</param>
        /// <param name="requestMethod">Request method or null for default</param>
        /// <returns>Result decoded from JSON response</returns>
        Task<T> MakeJsonRequestAsync<T>(string url, string baseUrl = null, Dictionary<string, object> payload = null, string requestMethod = null);

        #endregion Methods
    }
}
