using System.Collections.Concurrent;

namespace EzLiveServer.FileWatcher;

public class FileRegistry
{
    public string BaseDirectory { get; }
    public string DefaultFileExtension { get; }

    private readonly ConcurrentDictionary<string, FileCollection> filesRegistry;

    public FileRegistry(string directory, string defaultFileExtension)
    {
        BaseDirectory = directory.ToLowerInvariant();
        DefaultFileExtension = defaultFileExtension;

        filesRegistry = new();
        AddDirectoryIndices(directory);
    }

    public void AddIndex(string filePath, DateTime lastModified)
    {
        UnpackFilePath(filePath, out string directory, out string fileName, out string? fileExtension);

        if (filesRegistry.TryGetValue(directory, out FileCollection? fileCollection))
        {
            fileCollection.Add(fileName, fileExtension, lastModified);
        }
        else
        {
            var newCollection = new FileCollection(DefaultFileExtension);
            newCollection.Add(fileName, fileExtension, lastModified);
            _ = filesRegistry.TryAdd(directory, newCollection);
        }
    }

    public (string FilePath, DateTime LastModified)? GetIndex(string filePath)
    {
        UnpackFilePath(filePath, out string directory, out string fileName, out string? fileExtension);

        if (filesRegistry.TryGetValue(directory, out var fileCollection))
        {
            var fileIndex = fileCollection.Get(fileName, fileExtension);
            return fileIndex is null
                ? null
                : (Path.Combine(BaseDirectory, $"{directory}\\{fileIndex.FileName}.{fileIndex.Extension}"), fileIndex.LastModified);
        }

        return null;
    }

    public bool RemoveIndex(string filePath)
    {
        UnpackFilePath(filePath, out string directory, out string fileName, out string? fileExtension);

        if (filesRegistry.TryGetValue(directory, out var fileCollection))
        {
            bool fileRemoved = fileCollection.Remove(fileName, fileExtension);
            if (fileRemoved && fileCollection.Count == 0)
                filesRegistry.TryRemove(directory, out _);

            return fileRemoved;
        }

        return false;
    }

    public bool RemoveCollectionIndex(string index)
    {
        index = NormalizeIndex(index);

        return filesRegistry.TryRemove(index, out _);
    }

    public bool UpdateIndex(string oldPath, string newPath)
    {
        if (!RemoveIndex(oldPath))
            return false;

        AddIndex(newPath, DateTime.UtcNow);
        return true;
    }

    public void UpdateIndexLastModifiedDate(string index, DateTime newDate)
    {
        UnpackFilePath(index, out string directory, out string fileName, out string? fileExtension);

        if (filesRegistry.TryGetValue(directory, out var fileCollection))
            fileCollection.UpdateLastModifiedDate(fileName, fileExtension, newDate);
    }

    public bool UpdateCollectionIndex(string oldIndex, string newIndex)
    {
        oldIndex = NormalizeIndex(oldIndex);
        newIndex = NormalizeIndex(newIndex);

        return filesRegistry.TryRemove(oldIndex, out var tempCollection) && filesRegistry.TryAdd(newIndex, tempCollection);
    }

    protected static string NormalizeIndex(string index)
    {
        if (index.Length == 0)
            return "/";

        string normalized = index.Replace("\\", "/").ToLowerInvariant();

        normalized = normalized[0] != '/' ? '/' + normalized : normalized;
        normalized = normalized[^1] == '/' ? normalized[..^1] : normalized;

        return normalized;
    }

    protected void AddDirectoryIndices(string directory)
    {
        string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            AddIndex(file, new FileInfo(file).LastWriteTimeUtc);
        }
    }

    private void UnpackFilePath(string filePath, out string directory, out string fileName, out string? fileExtension)
    {
        filePath = filePath.ToLowerInvariant();
        bool relativeToBase = Path.GetDirectoryName(filePath)?.Length > 1 && filePath[0] != '/';
        string relativePath = Path.GetRelativePath(relativeToBase ? BaseDirectory : "/", filePath);

        directory = NormalizeIndex(Path.GetDirectoryName(relativePath)!);
        fileName = Path.GetFileNameWithoutExtension(filePath);
        fileExtension = Path.GetExtension(filePath)?.Length == 0 ? null : Path.GetExtension(filePath)[1..];
    }
}
