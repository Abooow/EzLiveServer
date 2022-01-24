using System.Collections.Concurrent;

namespace EzLiveServer;

public class FileRegistry
{
    public string BaseDirectory { get; }
    public string DefaultFileExtension { get; }

    private readonly ConcurrentDictionary<string, FileCollection> filesRegistry;

    public FileRegistry(string defaultFileExtension)
    {
        DefaultFileExtension = defaultFileExtension;
        BaseDirectory = "/";

        filesRegistry = new();
    }

    public FileRegistry(string defaultFileExtension, string directory)
        : this(defaultFileExtension)
    {
        BaseDirectory = directory.ToLowerInvariant();
        Initialize(directory);
    }

    public void AddIndex(string filePath)
    {
        UnpackFilePath(filePath, BaseDirectory, out string directory, out string fileName, out string? fileExtension);
        string key = $"{directory}/";

        if (filesRegistry.TryGetValue(key, out var fileCollection))
        {
            fileCollection.Add(fileName, fileExtension);
        }
        else
        {
            var newCollection = new FileCollection(DefaultFileExtension);
            newCollection.Add(fileName, fileExtension);
            _ = filesRegistry.TryAdd(key, newCollection);
        }
    }

    public string? GetIndex(string filePath)
    {
        UnpackFilePath(filePath, "/", out string directory, out string fileName, out string? fileExtension);
        string key = $"{directory}/";

        if (filesRegistry.TryGetValue(key, out var fileCollection))
        {
            var fileIndex = fileCollection.Get(fileName, fileExtension);
            return fileIndex is null ? null : Path.Combine(BaseDirectory, $"{directory}\\{fileIndex.FileName}.{fileIndex.Extension}");
        }

        return null;
    }

    public bool Remove(string filePath)
    {
        UnpackFilePath(filePath, "/", out string directory, out string fileName, out string? fileExtension);
        string key = $"{directory}/";

        if (filesRegistry.TryGetValue(key, out var fileCollection))
        {
            bool fileRemoved = fileCollection.Remove(fileName, fileExtension);
            if (fileRemoved && fileCollection.Count == 0)
                filesRegistry.TryRemove(key, out _);

            return fileRemoved;
        }

        return false;
    }

    public bool UpdateIndex(string oldPath, string newPath)
    {
        if (!Remove(oldPath))
            return false;

        AddIndex(newPath);
        return true;
    }

    private void Initialize(string directory)
    {
        string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            AddIndex(file);
        }
    }

    private static void UnpackFilePath(string filePath, string basePath, out string directory, out string fileName, out string? fileExtension)
    {
        filePath = filePath.ToLowerInvariant();
        string relativePath = Path.GetRelativePath(basePath, filePath);

        directory = Path.GetDirectoryName(relativePath)!;
        fileName = Path.GetFileNameWithoutExtension(filePath);
        fileExtension = Path.GetExtension(filePath)?.Length == 0 ? null : Path.GetExtension(filePath)[1..];
    }
}
