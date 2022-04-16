namespace EzLiveServer.Options;

public class FileServerOptionsBuilder
{
    private int? port;
    private readonly HashSet<string> uriPrefixes = new();
    private string? baseDirectory;
    private string? defaultNotFoundFilePath;
    private string? injectedFilePath;

    public FileServerOptionsBuilder WithPort(int port)
    {
        this.port = port;
        return this;
    }

    public FileServerOptionsBuilder WithUriPrefixes(params string[] uriPrefixes)
    {
        foreach (string uri in uriPrefixes)
        {
            this.uriPrefixes.Add(uri);
        }
        return this;
    }

    public FileServerOptionsBuilder WithBaseDirectory(string baseDirectory)
    {
        this.baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
        return this;
    }

    public FileServerOptionsBuilder WithDefaultNotFoundFilePath(string? defaultNotFoundFilePath)
    {
        this.defaultNotFoundFilePath = defaultNotFoundFilePath;
        return this;
    }

    public FileServerOptionsBuilder WithInjectedFilePath(string? injectedFilePath)
    {
        this.injectedFilePath = injectedFilePath;
        return this;
    }

    public FileServerOptions Build()
    {
        if (baseDirectory is null)
            throw new Exception($"{nameof(baseDirectory)} must be set before building. Consider using the {nameof(WithBaseDirectory)} method to set the base directory.");

        if (port is not null)
            uriPrefixes.Add($"http://localhost:{port}");

        return new FileServerOptions(baseDirectory, uriPrefixes, defaultNotFoundFilePath, injectedFilePath);
    }
}
