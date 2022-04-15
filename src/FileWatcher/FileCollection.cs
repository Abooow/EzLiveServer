using System.Collections.Concurrent;

namespace EzLiveServer.FileWatcher;

public class FileCollection
{
    public int Count => fileIndices.Count;

    private readonly ConcurrentDictionary<string, FileIndex> fileIndices;
    private readonly string defaultFileExtension;

    public FileCollection()
        : this("html")
    {
    }

    public FileCollection(string defaultFileExtension)
    {
        this.defaultFileExtension = defaultFileExtension.ToLowerInvariant();
        fileIndices = new();
    }

    public void Add(string fileName, string? extension, DateTime lastModified)
    {
        fileName = fileName.ToLowerInvariant();
        extension = extension?.ToLowerInvariant() ?? defaultFileExtension;

        fileIndices.TryAdd($"{fileName}.{extension}", new FileIndex(fileName, extension, lastModified));
    }

    public bool Remove(string fileName, string? extension)
    {
        fileName = fileName.ToLowerInvariant();
        extension = extension?.ToLowerInvariant() ?? defaultFileExtension;

        return fileIndices.TryRemove($"{fileName}.{extension}", out _);
    }

    public FileIndex? Get(string fileName, string? extension)
    {
        fileName = fileName.ToLowerInvariant();
        extension = extension?.ToLowerInvariant() ?? defaultFileExtension;

        bool found = fileIndices.TryGetValue($"{fileName}.{extension}", out var fileIndex);
        return found ? fileIndex : null;
    }

    public void UpdateLastModifiedDate(string fileName, string? extension, DateTime newDate)
    {
        fileName = fileName.ToLowerInvariant();
        extension = extension?.ToLowerInvariant() ?? defaultFileExtension;

        bool found = fileIndices.TryGetValue($"{fileName}.{extension}", out var oldIndex);
        if (found)
            fileIndices.TryUpdate($"{fileName}.{extension}", oldIndex! with { LastModified = newDate }, oldIndex);
    }
}
