using System.Net;
using System.Text;

namespace EzLiveServer;

public sealed class Server : IDisposable
{
    public string[] Prefixes { get; private set; }

    private readonly HttpListener httpListener;
    private readonly bool useRandomPort;

    private readonly CancellationTokenSource cancellationTokenSource;

    public Server()
    {
        httpListener = new HttpListener();
        useRandomPort = true;
        SetServerPrefixes(GetNewLocalhostPrefixWithRandomPort());

        cancellationTokenSource = new CancellationTokenSource();
    }

    public Server(params string[] prefixes)
    {
        httpListener = new HttpListener();
        SetServerPrefixes(prefixes);

        cancellationTokenSource = new CancellationTokenSource();
    }

    public void Start()
    {
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

    public async Task StartListenToRequestsAsync()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            var context = await httpListener.GetContextAsync().ConfigureAwait(false);

            string body = "<h1>Hello EzLiveServer!</h1>";
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            context.Response.ContentType = "text/html";
            await context.Response.OutputStream.WriteAsync(bodyBytes, cancellationTokenSource.Token);
            await context.Response.OutputStream.FlushAsync(cancellationTokenSource.Token);

            context.Response.Close();
        }
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
        try
        {
            httpListener.Stop();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            cancellationTokenSource.Cancel();
        }
    }
}
