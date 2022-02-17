using System.Net;
using System.Web;

namespace EzLiveServer;

public sealed class FileServer : Server
{
    private readonly FileRegistryWatcher fileRegistryWatcher;
    private readonly WebSocketServer webSocketServer;
    private int wsCounter;

    public FileServer(string baseDirectory)
        : this(baseDirectory, null)
    {
    }

    public FileServer(string baseDirectory, int port)
        : this(baseDirectory, (int?)port)
    {
    }

    private FileServer(string baseDirectory, int? port)
        : base(port)
    {
        fileRegistryWatcher = new(baseDirectory, "html");
        webSocketServer = new();

        webSocketServer.MessageRecived += (id, message) =>
        {
            if (message == "PING")
                webSocketServer.SendMessage("PONG", id);

            return Task.CompletedTask;
        };

        fileRegistryWatcher.FileContentChanged += FileContentChangedEvent;
        fileRegistryWatcher.IndexCollectionChanged += IndexCollectionChangedEvent;
        fileRegistryWatcher.IndexChanged += IndexChangedEvent;
    }

    protected override void StartInternal()
    {
        fileRegistryWatcher.StartWatching();
        base.StartInternal();
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
        string? filePath = fileRegistryWatcher.GetIndex(url);

        Console.WriteLine($"{url}");

        if (filePath is null)
        {
            string notFoundHtmlFile = fileRegistryWatcher.BaseDirectory + "/404.html";
            await HttpResponse.NotFoundAsync(listenerContext.Response, url, notFoundHtmlFile, CancellationTokenSource.Token);
        }
        else if (Path.GetExtension(filePath) == ".html")
        {
            string path = fileRegistryWatcher.BaseDirectory + filePath;
            await HttpResponse.FromCodeInjectedHtmlFileAsync(listenerContext.Response, path, CancellationTokenSource.Token);
        }
        else
        {
            string path = fileRegistryWatcher.BaseDirectory + filePath;
            await HttpResponse.FromFileAsync(listenerContext.Response, path, CancellationTokenSource.Token);
        }
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
