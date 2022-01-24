using System.Net;
using System.Text;

namespace EzLiveServer;

public class FileServer : Server
{
    private readonly FileRegistryWatcher fileRegistryWatcher;

    public FileServer(string baseDirectory)
    {
        fileRegistryWatcher = new(baseDirectory, "html");

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
        string body = "<h1>Hello FileServer!</h1>";
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        listenerContext.Response.ContentType = "text/html";
        await listenerContext.Response.OutputStream.WriteAsync(bodyBytes, CancellationTokenSource.Token);
        await listenerContext.Response.OutputStream.FlushAsync(CancellationTokenSource.Token);

        listenerContext.Response.Close();
    }

    private void FileContentChangedEvent(string obj)
    {
    }

    private void IndexCollectionChangedEvent(string arg1, string arg2)
    {
    }

    private void IndexChangedEvent(string arg1, string arg2)
    {
    }
}
