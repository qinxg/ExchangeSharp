﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Centipede
{
    /// <summary>
    /// Handles all the logic for making API calls.
    /// </summary>
    /// <seealso cref="Centipede.IAPIRequestMaker" />
    public sealed class APIRequestMaker : IAPIRequestMaker
    {
        private readonly IAPIRequestHandler _api;

        private class InternalHttpWebRequest : IHttpWebRequest
        {
            internal readonly HttpWebRequest Request;

            public InternalHttpWebRequest(Uri fullUri)
            {
                Request = WebRequest.Create(fullUri) as HttpWebRequest;
                Request.KeepAlive = false;

#if DEBUG
                Request.Proxy = new WebProxy("127.0.0.1", 1080);
#endif
            }

            public void AddHeader(string header, string value)
            {
                switch (header.ToStringLowerInvariant())
                {
                    case "content-type":
                        Request.ContentType = value;
                        break;

                    case "content-length":
                        Request.ContentLength = value.ConvertInvariant<long>();
                        break;

                    case "user-agent":
                        Request.UserAgent = value;
                        break;

                    case "accept":
                        Request.Accept = value;
                        break;

                    case "connection":
                        Request.Connection = value;
                        break;

                    default:
                        Request.Headers[header] = value;
                        break;
                }
            }

            public Uri RequestUri
            {
                get { return Request.RequestUri; }
            }

            public string Method
            {
                get { return Request.Method; }
                set { Request.Method = value; }
            }

            public int Timeout
            {
                get { return Request.Timeout; }
                set { Request.Timeout = value; }
            }

            public int ReadWriteTimeout
            {
                get { return Request.ReadWriteTimeout; }
                set { Request.ReadWriteTimeout = value; }
            }

            public async Task WriteAllAsync(byte[] data, int index, int length)
            {
                using (Stream stream = await Request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
        }

        private class InternalHttpWebResponse : IHttpWebResponse
        {
            private readonly HttpWebResponse response;

            public InternalHttpWebResponse(HttpWebResponse response)
            {
                this.response = response;
            }

            public IReadOnlyList<string> GetHeader(string name)
            {
                return response.Headers.GetValues(name) ?? CryptoUtility.EmptyStringArray;
            }

            public Dictionary<string, IReadOnlyList<string>> Headers
            {
                get
                {
                    Dictionary<string, IReadOnlyList<string>> headers = new Dictionary<string, IReadOnlyList<string>>();
                    foreach (var header in response.Headers.AllKeys)
                    {
                        headers[header] = new List<string>(response.Headers.GetValues(header));
                    }
                    return headers;
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="api">API</param>
        public APIRequestMaker(IAPIRequestHandler api)
        {
            this._api = api;
        }

        /// <summary>
        /// Make a request to a path on the API
        /// </summary>
        /// <param name="url">Path and query</param>
        /// <param name="baseUrl">Override the base url, null for the default BaseUrl</param>
        /// <param name="payload">Payload, can be null. For private API end points, the payload must contain a 'nonce' key set to GenerateNonce value.</param>
        /// The encoding of payload is API dependant but is typically json.</param>
        /// <param name="method">Request method or null for default. Example: 'GET' or 'POST'.</param>
        /// <returns>Raw response</returns>
        public async Task<string> MakeRequestAsync(string url, string baseUrl = null, Dictionary<string, object> payload = null, string method = null)
        {
            await new SynchronizationContextRemover();

            await _api.RateLimit.WaitToProceedAsync();
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }
            else if (url[0] != '/')
            {
                url = "/" + url;
            }

            string fullUrl = (baseUrl ?? _api.BaseUrl) + url;
            method = method ?? _api.RequestMethod;
            Uri uri = _api.ProcessRequestUrl(new UriBuilder(fullUrl), payload, method);
            InternalHttpWebRequest request = new InternalHttpWebRequest(uri)
            {
                Method = method
            };
            request.AddHeader("accept-language", "en-US,en;q=0.5");
            request.AddHeader("content-type", _api.RequestContentType);
            request.AddHeader("user-agent", BaseAPI.RequestUserAgent);
            request.Timeout = request.ReadWriteTimeout = (int)_api.RequestTimeout.TotalMilliseconds;
            
            await _api.ProcessRequestAsync(request, payload);
            HttpWebResponse response = null;
            string responseString = null;

            try
            {
                try
                {
                    RequestStateChanged?.Invoke(this, RequestMakerState.Begin, uri.AbsoluteUri);// when start make a request we send the uri, this helps developers to track the http requests.
                    response = await request.Request.GetResponseAsync() as HttpWebResponse;
                    if (response == null)
                    {
                        throw new APIException("Unknown response from server");
                    }
                }
                catch (WebException we)
                {
                    response = we.Response as HttpWebResponse;
                    if (response == null)
                    {
                        throw new APIException(we.Message ?? "Unknown response from server");
                    }
                }
                using (Stream responseStream = response.GetResponseStream())
                {
                    responseString = new StreamReader(responseStream).ReadToEnd();
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        // 404 maybe return empty responseString
                        if (string.IsNullOrWhiteSpace(responseString))
                        {
                            throw new APIException(
                                $"{response.StatusCode.ConvertInvariant<int>()} - {response.StatusCode}");
                        }
                        throw new APIException(responseString);
                    }
                    _api.ProcessResponse(new InternalHttpWebResponse(response));
                    RequestStateChanged?.Invoke(this, RequestMakerState.Finished, responseString);
                }
            }
            catch (Exception ex)
            {
                RequestStateChanged?.Invoke(this, RequestMakerState.Error, ex);
                throw;
            }
            finally
            {
                response?.Dispose();
            }
            return responseString;
        }

        /// <summary>
        /// An action to execute when a request has been made (this request and state and object (response or exception))
        /// </summary>
        public Action<IAPIRequestMaker, RequestMakerState, object> RequestStateChanged { get; set; }
    }
}
