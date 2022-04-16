using System.Net;
using System.Text;

namespace EzLiveServer;

public class Server : IDisposable
{
    public string[] UriPrefixes { get; private set; }

    protected readonly CancellationTokenSource CancellationTokenSource;
    private readonly HttpListener httpListener;

    public Server(int port)
        : this($"http://localhost:{port}/")
    {
    }

    public Server(params string[] uriPrefixes)
    {
        httpListener = new HttpListener();
        CancellationTokenSource = new CancellationTokenSource();

        SetServerUriPrefixes(uriPrefixes);
        UriPrefixes = httpListener.Prefixes.ToArray();
    }

    public void Start()
    {
        StartInternal();
    }

    protected virtual void StartInternal()
    {
        httpListener.Start();
    }

    public async Task StartListenToRequestsAsync()
    {
        while (!CancellationTokenSource.Token.IsCancellationRequested)
        {
            var context = await httpListener.GetContextAsync().ConfigureAwait(false);

            _ = Task.Run(() => HandleRequestAsync(context).ConfigureAwait(false));
        }
    }

    protected virtual async Task HandleRequestAsync(HttpListenerContext listenerContext)
    {
        string body = "<h1>Hello EzLiveServer!</h1>";
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        listenerContext.Response.ContentType = "text/html";
        await listenerContext.Response.OutputStream.WriteAsync(bodyBytes, CancellationTokenSource.Token);
        await listenerContext.Response.OutputStream.FlushAsync(CancellationTokenSource.Token);

        listenerContext.Response.Close();
    }

    private void SetServerUriPrefixes(string[] uriPrefixes)
    {
        httpListener.Prefixes.Clear();

        foreach (string uri in uriPrefixes)
        {
            string fixedPrefix = uri[^1] == '/' ? uri : uri + '/';
            httpListener.Prefixes.Add(fixedPrefix);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
            httpListener.Stop();
            httpListener.Close();
        }
    }
}
