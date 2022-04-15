﻿using System.Net;
using System.Web;
using EzLiveServer.FileWatcher;
using EzLiveServer.WebSockets;

namespace EzLiveServer;

public sealed class FileServer : Server
{
    private readonly FileRegistryWatcher fileRegistryWatcher;
    private readonly WebSocketServer webSocketServer;
    private int wsCounter;

    public FileServer(string baseDirectory, int port)
        : this(baseDirectory, $"http://localhost:{port}/")
    {
    }

    public FileServer(string baseDirectory, params string[] prefixes)
        : base(prefixes)
    {
        fileRegistryWatcher = new(baseDirectory, "html");
        webSocketServer = new();

        webSocketServer.MessageRecived += HandleWebsocketMessage;

        fileRegistryWatcher.FileContentChanged += FileContentChangedEvent;
        fileRegistryWatcher.IndexCollectionChanged += IndexCollectionChangedEvent;
        fileRegistryWatcher.IndexChanged += IndexChangedEvent;
    }

    private Task HandleWebsocketMessage(int websocketId, string message)
    {
        if (message == "PING")
            webSocketServer.SendMessage("PONG", websocketId);

        return Task.CompletedTask;
    }

    protected override void StartInternal()
    {
        base.StartInternal();
        fileRegistryWatcher.StartWatching();
    }

    protected override async Task HandleRequestAsync(HttpListenerContext listenerContext)
    {
        if (listenerContext.Request.IsWebSocketRequest)
        {
            var wsContext = await listenerContext.AcceptWebSocketAsync(null);
            int socketId = Interlocked.Increment(ref wsCounter);
            webSocketServer.ProcessWebSocket(socketId, wsContext.WebSocket);
            return;
        }

        string url = listenerContext.Request.Url!.AbsolutePath == "/" ? "/index.html" : HttpUtility.UrlDecode(listenerContext.Request.Url.AbsolutePath)!;
        var file = fileRegistryWatcher.GetIndex(url);

        Console.WriteLine($"{url}");

        if (file is null)
        {
            string notFoundHtmlFile = fileRegistryWatcher.BaseDirectory + "/404.html";
            await HttpResponse.NotFoundAsync(listenerContext.Response, url, notFoundHtmlFile, CancellationTokenSource.Token);
            return;
        }

        HttpResponse.LastModified(listenerContext.Response, file.Value.LastModified);

        if (!RequestedResourceHasBeenUpdated(listenerContext.Request, file.Value.LastModified))
        {
            HttpResponse.NotModified(listenerContext.Response);
            return;
        }

        string path = fileRegistryWatcher.BaseDirectory + file.Value.FilePath;
        if (Path.GetExtension(path) == ".html")
            await HttpResponse.FromCodeInjectedHtmlFileAsync(listenerContext.Response, path, CancellationTokenSource.Token);
        else
            await HttpResponse.FromFileAsync(listenerContext.Response, path, CancellationTokenSource.Token);
    }

    private static bool RequestedResourceHasBeenUpdated(HttpListenerRequest request, DateTime lastModified)
    {
        string? lastModifiedHeader = request.Headers["If-Modified-Since"];
        if (lastModifiedHeader is null)
            return true;

        var clientLastModifiedDate = DateTime.Parse(lastModifiedHeader).ToUniversalTime();
        lastModified = new DateTime(lastModified.Ticks - (lastModified.Ticks % TimeSpan.TicksPerSecond), lastModified.Kind); // Remove milliseconds.
        return lastModified > clientLastModifiedDate;
    }

    private void FileContentChangedEvent(string path)
    {
        string extension = Path.GetExtension(path);
        string message = extension switch
        {
            ".html" or ".js" => $"reload {path}",
            ".css" => $"refreshcss {path}",
            _ => $"updated {path}"
        };

        webSocketServer.Broadcast(message);
    }

    private void IndexCollectionChangedEvent(string oldIndexCollection, string newIndexCollection)
    {
    }

    private void IndexChangedEvent(string oldIndex, string newIndex)
    {
    }

    protected override void Dispose(bool disposing)
    {
        fileRegistryWatcher.Dispose();
        webSocketServer.DisposeAsync()
            .GetAwaiter()
            .GetResult();

        base.Dispose(disposing);
    }

    ~FileServer()
    {
        Dispose(false);
    }
}
