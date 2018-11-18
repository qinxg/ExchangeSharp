using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Centipede
{
    /// <summary>
    ///     Wraps a web socket for easy dispose later, along with auto-reconnect and message and reader queues
    /// </summary>
    public sealed class ClientWebSocket : IWebSocket
    {
        private const int ReceiveChunkSize = 8192;

        private static Func<IClientWebSocketImplementation> _webSocketCreator =
            () => new ClientWebSocketImplementation();

        private readonly CancellationToken _cancellationToken;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly BlockingCollection<object> _messageQueue =
            new BlockingCollection<object>(new ConcurrentQueue<object>());

        private TimeSpan _keepAlive = TimeSpan.FromSeconds(30.0);

        // created from factory, allows swapping out underlying implementation
        private IClientWebSocketImplementation _webSocket;

        private bool _disposed;

        /// <summary>
        ///     Default constructor, does not begin listening immediately. You must set the properties and then call Start.
        /// </summary>
        public ClientWebSocket()
        {
            _cancellationToken = _cancellationTokenSource.Token;
        }

        /// <summary>
        ///     The uri to connect to
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        ///     Action to handle incoming text messages. If null, text messages are handled with OnBinaryMessage.
        /// </summary>
        public Func<IWebSocket, string, Task> OnTextMessage { get; set; }

        /// <summary>
        ///     Action to handle incoming binary messages
        /// </summary>
        public Func<IWebSocket, byte[], Task> OnBinaryMessage { get; set; }

        /// <summary>
        ///     Whether to close the connection gracefully, this can cause the close to take longer.
        /// </summary
        public bool CloseCleanly { get; set; }

        /// <summary>
        ///     Interval to call connect at regularly (default is 1 hour)
        /// </summary>
        public TimeSpan ConnectInterval { get; set; } = TimeSpan.FromHours(1.0);

        /// <summary>
        ///     Keep alive interval (default is 30 seconds)
        /// </summary>
        public TimeSpan KeepAlive
        {
            get => _keepAlive;
            set
            {
                _keepAlive = value;

                if (_webSocket != null) _webSocket.KeepAliveInterval = value;
            }
        }

        /// <summary>
        ///     Allows additional listeners for connect event
        /// </summary>
        public event WebSocketConnectionDelegate Connected;

        /// <summary>
        ///     Allows additional listeners for disconnect event
        /// </summary>
        public event WebSocketConnectionDelegate Disconnected;

        /// <summary>
        ///     Close and dispose of all resources, stops the web socket and shuts it down.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cancellationTokenSource.Cancel();
                Task.Run(async () =>
                {
                    try
                    {
                        if (_webSocket.State == WebSocketState.Open)
                        {
                            if (CloseCleanly)
                                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Dispose",
                                    _cancellationToken);
                            else
                                await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Dispose",
                                    _cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // dont care
                    }
                    catch (Exception ex)
                    {
                        Logger.Info(ex.ToString());
                    }
                });
            }
        }

        /// <summary>
        ///     Queue a message to the WebSocket server, it will be sent as soon as possible.
        /// </summary>
        /// <param name="message">Message to send, can be string, byte[] or object (which get json serialized)</param>
        /// <returns>True if success, false if error</returns>
        public Task<bool> SendMessageAsync(object message)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                QueueActions(async socket =>
                {
                    byte[] bytes;
                    WebSocketMessageType messageType;
                    if (message is string s)
                    {
                        bytes = s.ToBytesUTF8();
                        messageType = WebSocketMessageType.Text;
                    }
                    else if (message is byte[] b)
                    {
                        bytes = b;
                        messageType = WebSocketMessageType.Binary;
                    }
                    else
                    {
                        bytes = JsonConvert.SerializeObject(message).ToBytesUTF8();
                        messageType = WebSocketMessageType.Text;
                    }

                    var messageArraySegment = new ArraySegment<byte>(bytes);
                    await _webSocket.SendAsync(messageArraySegment, messageType, true, _cancellationToken);
                });
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        private void CreateWebSocket()
        {
            _webSocket = _webSocketCreator();
        }

        /// <summary>
        ///     Register a function that will be responsible for creating the underlying web socket implementation
        ///     By default, C# built-in web sockets are used (Windows 8.1+ required). But you could swap out
        ///     a different web socket for other platforms, testing, or other specialized needs.
        /// </summary>
        /// <param name="creator">Creator function. Pass null to go back to the default implementation.</param>
        public static void RegisterWebSocketCreator(Func<IClientWebSocketImplementation> creator)
        {
            if (creator == null)
                _webSocketCreator = () => new ClientWebSocketImplementation();
            else
                _webSocketCreator = creator;
        }

        /// <summary>
        ///     Start the web socket listening and processing
        /// </summary>
        public void Start()
        {
            CreateWebSocket();

            // kick off message parser and message listener
            Task.Run(MessageTask);


           //消息接受
            Task.Run(ReadTask);
        }

        private void QueueActions(params Func<IWebSocket, Task>[] actions)
        {
            if (actions != null && actions.Length != 0)
            {
                var actionsCopy = actions;
                _messageQueue.Add((Func<Task>) (async () =>
                {
                    foreach (var action in actionsCopy.Where(a => a != null))
                        try
                        {
                            await action.Invoke(this);
                        }
                        catch (Exception ex)  //这里吃异常是为了方法继续执行，不影响队列
                        {
                            Logger.Info(ex.ToString());
                        }
                }));
            }
        }

        private void QueueActionsWithNoExceptions(params Func<IWebSocket, Task>[] actions)
        {
            if (actions != null && actions.Length != 0)
            {
                var actionsCopy = actions;
                _messageQueue.Add((Func<Task>) (async () =>
                {
                    foreach (var action in actionsCopy.Where(a => a != null))
                    {
                        while (!_disposed)
                        {
                            try
                            {
                                await action.Invoke(this);
                                break;
                            }
                            catch (Exception ex) //这里吃异常是为了方法继续执行，不影响队列
                            {
                                Logger.Info(ex.ToString());
                            }
                        }
                    }
                }));
            }
        }

        private async Task InvokeConnected(IWebSocket socket)
        {
            var connected = Connected;
            if (connected != null)
            {
                await connected.Invoke(socket);
            }
        }

        private async Task InvokeDisconnected(IWebSocket socket)
        {
            var disconnected = Disconnected;
            if (disconnected != null) await disconnected.Invoke(this);
        }

        /// <summary>
        /// 消息读取
        /// </summary>
        /// <returns></returns>
        private async Task ReadTask()
        {
            var receiveBuffer = new ArraySegment<byte>(new byte[ReceiveChunkSize]);
            var keepAlive = _webSocket.KeepAliveInterval;
            var stream = new MemoryStream();
            var wasConnected = false;

            while (!_disposed)
            {
                try
                {
                    // open the socket
                    _webSocket.KeepAliveInterval = KeepAlive;
                    wasConnected = false;
                    await _webSocket.ConnectAsync(Uri, _cancellationToken);
                    while (!_disposed && _webSocket.State == WebSocketState.Connecting) await Task.Delay(20);
                    if (_disposed || _webSocket.State != WebSocketState.Open) continue;
                    wasConnected = true;

                    // on connect may make additional calls that must succeed, such as rest calls
                    // for lists, etc.
                    QueueActionsWithNoExceptions(InvokeConnected);

                    while (_webSocket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await _webSocket.ReceiveAsync(receiveBuffer, _cancellationToken);
                            if (result != null)
                            {
                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                                        _cancellationToken);
                                    QueueActions(InvokeDisconnected);
                                }
                                else
                                {
                                    stream.Write(receiveBuffer.Array, 0, result.Count);
                                }
                            }
                        } while (result != null && !result.EndOfMessage);

                        if (stream.Length != 0)
                        {
                            // if text message and we are handling text messages
                            if (result.MessageType == WebSocketMessageType.Text && OnTextMessage != null)
                            {
                                _messageQueue.Add(Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int) stream.Length));
                            }
                            // otherwise treat message as binary
                            else
                            {
                                // make a copy of the bytes, the memory stream will be re-used and could potentially corrupt in multi-threaded environments
                                // not using ToArray just in case it is making a slice/span from the internal bytes, we want an actual physical copy
                                var bytesCopy = new byte[stream.Length];
                                Array.Copy(stream.GetBuffer(), bytesCopy, stream.Length);
                                _messageQueue.Add(bytesCopy);
                            }

                            stream.SetLength(0);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // dont care
                }
                catch (Exception ex)  //这里吃异常是为了重新开始
                {
                    // eat exceptions, most likely a result of a disconnect, either way we will re-create the web socket
                    Logger.Info(ex.ToString());
                }

                //异常后，就触发失去连接 + 重连

                if (wasConnected) QueueActions(InvokeDisconnected);
                try
                {
                    _webSocket.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Info(ex.ToString());
                }

                if (!_disposed)
                {
                    // wait 5 seconds before attempting reconnect
                    CreateWebSocket();
                    await Task.Delay(5000);
                }
            }
        }

        private async Task MessageTask()
        {
            var lastCheck = CryptoUtility.UtcNow;

            while (!_disposed)
            {
                if (_messageQueue.TryTake(out var message, 100))
                    try
                    {

                        if (message is Func<Task> action)
                        {
                            await action();
                        }
                        else if (message is byte[] messageBytes)
                        {
                            // multi-thread safe null check
                            var actionCopy = OnBinaryMessage;
                            if (actionCopy != null) await actionCopy.Invoke(this, messageBytes);
                        }
                        else if (message is string messageString)
                        {
                            // multi-thread safe null check
                            var actionCopy = OnTextMessage;
                            if (actionCopy != null) await actionCopy.Invoke(this, messageString);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // dont care
                    }
                    catch (Exception ex)   
                    { 
                        //这里吃异常是为了方法继续执行，不影响队列 。
                        //前面都处理了，其实这里不需要在吃异常了
                        Logger.Info(ex.ToString());
                    }

                if (ConnectInterval.Ticks > 0 && CryptoUtility.UtcNow - lastCheck >= ConnectInterval)
                {
                    lastCheck = CryptoUtility.UtcNow;

                    // this must succeed, the callback may be requests lists or other resources that must not fail
                    QueueActionsWithNoExceptions(InvokeConnected);
                }
            }
        }

        /// <summary>
        ///     Client web socket implementation
        /// </summary>
        public interface IClientWebSocketImplementation : IDisposable
        {
            /// <summary>
            ///     Web socket state
            /// </summary>
            WebSocketState State { get; }

            /// <summary>
            ///     Keep alive interval (heartbeat)
            /// </summary>
            TimeSpan KeepAliveInterval { get; set; }

            /// <summary>
            ///     Close cleanly
            /// </summary>
            /// <param name="closeStatus">Status</param>
            /// <param name="statusDescription">Description</param>
            /// <param name="cancellationToken">Cancel token</param>
            /// <returns>Task</returns>
            Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
                CancellationToken cancellationToken);

            /// <summary>
            ///     Close output immediately
            /// </summary>
            /// <param name="closeStatus">Status</param>
            /// <param name="statusDescription">Description</param>
            /// <param name="cancellationToken">Cancel token</param>
            /// <returns>Task</returns>
            Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription,
                CancellationToken cancellationToken);

            /// <summary>
            ///     Connect
            /// </summary>
            /// <param name="uri">Uri</param>
            /// <param name="cancellationToken">Cancel token</param>
            /// <returns>Task</returns>
            Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

            /// <summary>
            ///     Receive
            /// </summary>
            /// <param name="buffer">Buffer</param>
            /// <param name="cancellationToken">Cancel token</param>
            /// <returns>Result</returns>
            Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken);

            /// <summary>
            ///     Send
            /// </summary>
            /// <param name="buffer">Buffer</param>
            /// <param name="messageType">Message type</param>
            /// <param name="endOfMessage">True if end of message, false otherwise</param>
            /// <param name="cancellationToken">Cancel token</param>
            /// <returns>Task</returns>
            Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
                CancellationToken cancellationToken);
        }

        private class ClientWebSocketImplementation : IClientWebSocketImplementation
        {
            private readonly System.Net.WebSockets.ClientWebSocket _webSocket =
                new System.Net.WebSockets.ClientWebSocket();

            public WebSocketState State => _webSocket.State;


            public ClientWebSocketImplementation()
            {
#if DEBUG
                _webSocket.Options.Proxy = new WebProxy("127.0.0.1",1080);
#endif
            }

            public TimeSpan KeepAliveInterval
            {
                get => _webSocket.Options.KeepAliveInterval;
                set => _webSocket.Options.KeepAliveInterval = value;
            }

            public void Dispose()
            {
                _webSocket.Dispose();
            }

            public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
                CancellationToken cancellationToken)
            {
                return _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
            }

            public Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription,
                CancellationToken cancellationToken)
            {
                return _webSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
            }

            public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
            {
                return _webSocket.ConnectAsync(uri, cancellationToken);
            }

            public Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
                CancellationToken cancellationToken)
            {
                return _webSocket.ReceiveAsync(buffer, cancellationToken);
            }

            public Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
                CancellationToken cancellationToken)
            {
                return _webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
            }
        }
    }

    /// <summary>
    ///     Delegate for web socket connect / disconnect events
    /// </summary>
    /// <param name="socket">Web socket</param>
    /// <returns>Task</returns>
    public delegate Task WebSocketConnectionDelegate(IWebSocket socket);

    /// <summary>
    ///     Web socket interface
    /// </summary>
    public interface IWebSocket : IDisposable
    {
        /// <summary>
        ///     Interval to call connect at regularly (default is 1 hour)
        /// </summary>
        TimeSpan ConnectInterval { get; set; }

        /// <summary>
        ///     Keep alive interval (default varies by exchange)
        /// </summary>
        TimeSpan KeepAlive { get; set; }

        /// <summary>
        ///     Connected event
        /// </summary>
        event WebSocketConnectionDelegate Connected;

        /// <summary>
        ///     Disconnected event
        /// </summary>
        event WebSocketConnectionDelegate Disconnected;

        /// <summary>
        ///     Send a message over the web socket
        /// </summary>
        /// <param name="message">Message to send, can be string, byte[] or object (which get serialized to json)</param>
        /// <returns>True if success, false if error</returns>
        Task<bool> SendMessageAsync(object message);
    }
}