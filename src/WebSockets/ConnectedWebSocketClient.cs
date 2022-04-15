using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace EzLiveServer.WebSockets;

internal class ConnectedWebSocketClient
{
    public int WebSocketId { get; }
    public WebSocket WebSocket { get; }
    public CancellationTokenSource BroadcastLoopTokenSource { get; set; }

    private readonly BlockingCollection<string> broadcastQueue;
    private readonly int broadcastTransmitInterval;

    public ConnectedWebSocketClient(int webSocketId, WebSocket webSocket, int broadcastTransmitInterval)
    {
        WebSocketId = webSocketId;
        WebSocket = webSocket;
        BroadcastLoopTokenSource = new();

        broadcastQueue = new();
        this.broadcastTransmitInterval = broadcastTransmitInterval;
    }

    public void SendMessage(string message)
    {
        broadcastQueue.Add(message);
    }

    public async Task BroadcastLoopAsync()
    {
        var cancellationToken = BroadcastLoopTokenSource.Token;
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(broadcastTransmitInterval, cancellationToken);

            if (!cancellationToken.IsCancellationRequested && WebSocket.State == WebSocketState.Open && broadcastQueue.TryTake(out string? message))
            {
                var msgbuf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                await WebSocket.SendAsync(msgbuf, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
            }
        }
    }
}
