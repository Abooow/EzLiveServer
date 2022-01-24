namespace EzLiveServer;

public class FileCollection
{
    public int Count => fileIndices.Count;

    private readonly List<FileIndex> fileIndices;
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

    public void Add(string fileName, string? extension)
    {
        fileName = fileName.ToLowerInvariant();
        extension = extension?.ToLowerInvariant() ?? defaultFileExtension;

        fileIndices.Add(new FileIndex(fileName, extension));
    }

    public bool Remove(string fileName, string? extension)
    {
        fileName = fileName.ToLowerInvariant();
        extension = extension?.ToLowerInvariant() ?? defaultFileExtension;

        var fileIndex = fileIndices.Find(x => x.FileName == fileName && x.Extension == extension);
        return fileIndices.Remove(fileIndex!);
    }

    public FileIndex? Get(string fileName, string? extension)
    {
        fileName = fileName.ToLowerInvariant();
        extension = extension?.ToLowerInvariant() ?? defaultFileExtension;

        return fileIndices.Find(x => x.FileName == fileName && x.Extension == extension);
    }
}

public record FileIndex(string FileName, string Extension);
