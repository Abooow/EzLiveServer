using Cocona;

internal class FileServerCliParameters : ICommandParameterSet
{
    [Option('d', Description = "Specifies the root directory to serve static files from.")]
    public string Directory { get; set; } = default!;

    [Option('p', Description = "Port to listen to.")]
    [HasDefaultValue]
    public int Port { get; set; } = 5069;
}