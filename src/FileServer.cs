using System.Net;
using System.Web;

namespace EzLiveServer;

public class FileServer : Server
{
    private readonly FileRegistryWatcher fileRegistryWatcher;

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
        string url = listenerContext.Request.Url!.AbsolutePath == "/" ? "/index.html" : HttpUtility.UrlDecode(listenerContext.Request.Url.AbsolutePath)!;
        string? filePath = fileRegistryWatcher.GetIndex(url);

        Console.WriteLine($"{url}");

        if (filePath is null)
        {
            string notFoundHtmlFile = fileRegistryWatcher.BaseDirectory + "/404.html";
            await HttpResponse.NotFoundAsync(listenerContext.Response, url, notFoundHtmlFile, CancellationTokenSource.Token);
        }
        else
        {
            string path = fileRegistryWatcher.BaseDirectory + filePath;
            await HttpResponse.FromFileAsync(listenerContext.Response, path, CancellationTokenSource.Token);
        }
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

    protected override void Dispose(bool disposing)
    {
        fileRegistryWatcher.Dispose();

        base.Dispose(disposing);
    }

    ~FileServer()
    {
        Dispose(false);
    }
}
