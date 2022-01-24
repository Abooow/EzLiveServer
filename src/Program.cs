using EzLiveServer;

using var server = new FileServer(@"C:\Users\danie\source\js\EzLiveServerTest");

Console.WriteLine("Starting server...");
server.Start();

foreach (var prefix in server.Prefixes)
{
    Console.WriteLine($"Listening on: {prefix}");
}

await server.StartListenToRequestsAsync();