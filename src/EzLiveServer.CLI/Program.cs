using Cocona;
using EzLiveServer.Core;
using EzLiveServer.Core.Options;

await CoconaLiteApp.RunAsync(async (FileServerCliParameters cliParameters) =>
{
    var options = new FileServerOptionsBuilder()
        .WithInjectedFilePath("./StaticContent/InjectedToResponse.html")
        .WithDefaultNotFoundFilePath("./StaticContent/Default404NotFound.html")
        .WithBaseDirectory(cliParameters.Directory)
        .WithPort(cliParameters.Port)
        .Build();

    using var server = new FileServer(options);

    Console.WriteLine("Starting server...");
    server.Start();

    foreach (string uri in server.UriPrefixes)
    {
        Console.WriteLine($"Listening on: {uri}");
    }

    await server.StartListenToRequestsAsync();
});