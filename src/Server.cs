using System.Net;
using System.Text;

namespace EzLiveServer;

public class Server : IDisposable
{
    public string[] Prefixes { get; private set; }

    protected readonly CancellationTokenSource CancellationTokenSource;

    private readonly HttpListener httpListener;
    private readonly bool useRandomPort;

    public Server()
    {
        httpListener = new HttpListener();
        CancellationTokenSource = new CancellationTokenSource();
        useRandomPort = true;
    }

    public Server(int port)
        : this()
    {
        useRandomPort = false;
        SetServerPrefixes($"http://localhost:{port}/");
    }

    protected Server(int? port)
        : this()
    {
        useRandomPort = port is null;
        if (!useRandomPort)
            SetServerPrefixes($"http://localhost:{port}/");
    }

    public Server(params string[] prefixes)
        : this()
    {
        useRandomPort = false;
        SetServerPrefixes(prefixes);
    }

    public void Start()
    {
        StartInternal();
    }

    public async Task StartListenToRequestsAsync()
    {
        while (!CancellationTokenSource.Token.IsCancellationRequested)
        {
            var context = await httpListener.GetContextAsync().ConfigureAwait(false);

            _ = Task.Run(() => HandleRequestAsync(context).ConfigureAwait(false));
        }
    }

    protected virtual void StartInternal()
    {
        if (Prefixes is null)
            SetServerPrefixes(GetNewLocalhostPrefixWithRandomPort());

        bool serverStartupSuccess = false;

        do
        {
            try
            {
                httpListener.Start();
                serverStartupSuccess = true;
            }
            catch (HttpListenerException) when (useRandomPort)
            {
                SetServerPrefixes(GetNewLocalhostPrefixWithRandomPort());
            }
        } while (!serverStartupSuccess);
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

    private void SetServerPrefixes(params string[] prefixes)
    {
        httpListener.Prefixes.Clear();

        foreach (var prefix in prefixes)
        {
            string fixedPrefix = prefix[^1] == '/' ? prefix : prefix + '/';
            httpListener.Prefixes.Add(fixedPrefix);
        }

        Prefixes = prefixes;
    }

    private static string GetNewLocalhostPrefixWithRandomPort() => $"http://localhost:{Random.Shared.Next(3000, 9000)}/";

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
