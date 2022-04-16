namespace EzLiveServer.Options;

public sealed record FileServerOptions(string BaseDirectory, IEnumerable<string> UriPrefixes, string? DefaultNotFoundFilePath, string? InjectedFilePath);
