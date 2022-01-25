using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace EzLiveServer;

public sealed class WebSocketServer : IAsyncDisposable
{
    public event Func<int, string, Task>? MessageRecived;

    private readonly ConcurrentDictionary<int, ConnectedWebSocketClient> webSocketClients;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly int broadcastTransmitInterval;

    public WebSocketServer(int broadcastTransmitInterval = 250)
    {
        webSocketClients = new();
        cancellationTokenSource = new();
        this.broadcastTransmitInterval = broadcastTransmitInterval;
    }

    public void ProcessWebSocket(int socketId, WebSocket webSocket)
    {
        var client = new ConnectedWebSocketClient(socketId, webSocket, broadcastTransmitInterval);
        webSocketClients.TryAdd(socketId, client);
        _ = Task.Run(() => WebSocketProcessingLoopAsync(client).ConfigureAwait(false));
    }

    public void Broadcast(string message)
    {
        foreach (var client in webSocketClients)
        {
            client.Value.SendMessage(message);
        }
    }

    public void SendMessage(string message, int socketId)
    {
        if (webSocketClients.TryGetValue(socketId, out var client))
            client.SendMessage(message);
    }

    private async Task WebSocketProcessingLoopAsync(ConnectedWebSocketClient client)
    {
        _ = Task.Run(() => client.BroadcastLoopAsync().ConfigureAwait(false));

        var webSocket = client.WebSocket;
        var clientBroadcastTokenSource = client.BroadcastLoopTokenSource;

        try
        {
            var buffer = WebSocket.CreateServerBuffer(4096);
            while (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                var receiveResult = await client.WebSocket.ReceiveAsync(buffer, cancellationTokenSource.Token);
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (client.WebSocket.State == WebSocketState.CloseReceived && receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        client.BroadcastLoopTokenSource.Cancel();
                        await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge Close frame", CancellationToken.None);
                    }

                    if (client.WebSocket.State == WebSocketState.Open)
                    {
                        string message = Encoding.UTF8.GetString(buffer.Array!, 0, receiveResult.Count);
                        _ = Task.Run(() => MessageRecived?.Invoke(client.WebSocketId, message).ConfigureAwait(false));
                    }
                }
            }
        }
        finally
        {
            clientBroadcastTokenSource.Cancel();

            if (webSocket.State != WebSocketState.Closed)
                webSocket.Abort();

            if (webSocketClients.TryRemove(client.WebSocketId, out _))
                webSocket.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        var disposeWebSocketsList = new WebSocket[webSocketClients.Count];

        int i = 0;
        while (!webSocketClients.IsEmpty)
        {
            var client = webSocketClients.ElementAt(0).Value;

            client.BroadcastLoopTokenSource.Cancel();

            if (client.WebSocket.State != WebSocketState.Open)
            {
                var timeout = new CancellationTokenSource(2500);
                await client.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token);
            }

            if (webSocketClients.TryRemove(client.WebSocketId, out _))
                disposeWebSocketsList[i++] = client.WebSocket;
        }

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();

        foreach (var webSocket in disposeWebSocketsList)
            webSocket.Dispose();
    }
}
