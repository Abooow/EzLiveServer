using EzLiveServer.Core;
using EzLiveServer.Core.Options;

var options = new FileServerOptionsBuilder()
    .WithPort(5069)
    .WithBaseDirectory(@"/")
    .WithInjectedFilePath("./StaticContent/InjectedToResponse.html")
    .WithDefaultNotFoundFilePath("./StaticContent/Default404NotFound.html")
    .Build();

using var server = new FileServer(options);

Console.WriteLine("Starting server...");
server.Start();

foreach (string uri in server.UriPrefixes)
{
    Console.WriteLine($"Listening on: {uri}");
}

await server.StartListenToRequestsAsync();