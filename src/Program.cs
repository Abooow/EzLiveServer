using EzLiveServer;

using var server = new Server();

Console.WriteLine("Starting server...");
server.Start();

foreach (var prefix in server.Prefixes)
{
    Console.WriteLine($"Listening on: {prefix}");
}

await server.StartListenToRequestsAsync();