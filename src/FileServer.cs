using System.Net;
using System.Text;
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
        string url = listenerContext.Request.RawUrl == "/" ? "/index" : HttpUtility.UrlDecode(listenerContext.Request.RawUrl)!;
        string? filePath = fileRegistryWatcher.GetIndex(url);

        Console.WriteLine($"{url}");

        if (filePath is null)
            await WriteNotFoundToResponseAsync(url, listenerContext.Response);
        else
            await WriteFileToResponseAsync(filePath, listenerContext.Response);
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

    private async Task WriteNotFoundToResponseAsync(string url, HttpListenerResponse httpResponse)
    {
        httpResponse.ContentType = "text/html";
        httpResponse.StatusCode = 404;
        httpResponse.StatusDescription = "Not Found";

        if (File.Exists(fileRegistryWatcher.BaseDirectory + "/404.html"))
        {
            await WriteFileToResponseAsync("/404.html", httpResponse);
            return;
        }

        string body = $@"
<h1>404 Not Found</h1>
<p>
<strong>{url}</strong> does not exist.
<a href=""#"" onclick=""location.reload(true)"">Refresh</a>
<a href=""/"">Home</a>
</p>";

        var bodyBytes = Encoding.UTF8.GetBytes(body);

        await httpResponse.OutputStream.WriteAsync(bodyBytes, CancellationTokenSource.Token);
        await httpResponse.OutputStream.FlushAsync(CancellationTokenSource.Token);

        httpResponse.Close();
    }

    private async Task WriteFileToResponseAsync(string file, HttpListenerResponse httpResponse)
    {
        string path = fileRegistryWatcher.BaseDirectory + file;
        var fileBytes = await File.ReadAllBytesAsync(path, CancellationTokenSource.Token);

        httpResponse.ContentType = GetContentType(Path.GetExtension(file));
        await httpResponse.OutputStream.WriteAsync(fileBytes, CancellationTokenSource.Token);
        await httpResponse.OutputStream.FlushAsync(CancellationTokenSource.Token);

        httpResponse.Close();
    }

    private static string GetContentType(string fileExtension)
    {
        return fileExtension switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "text/javascript",
            ".txt" => "text/plain",
            ".ico" => "text/x-icon",
            ".png" => "image/png",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/html",
            ".json" => "application/json",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
