namespace EzLiveServer;

public sealed class FileRegistryWatcher : FileRegistry, IDisposable
{
    public event Action<string>? FileContentChanged;
    public event Action<string, string>? IndexCollectionChanged;
    public event Action<string, string>? IndexChanged;

    private readonly FileSystemWatcher fileSystemWatcher;
    private readonly Dictionary<string, long> lastFileChangeTime;
    private readonly long updateRateTicks;

    public FileRegistryWatcher(string directory, string defaultFileExtension)
        : base(directory, defaultFileExtension)
    {
        fileSystemWatcher = new(directory)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            IncludeSubdirectories = true
        };

        fileSystemWatcher.Changed += OnChanged;
        fileSystemWatcher.Created += OnCreated;
        fileSystemWatcher.Deleted += OnDeleted;
        fileSystemWatcher.Renamed += OnRenamed;

        fileSystemWatcher.Filters.Add("*");

        lastFileChangeTime = new();
        updateRateTicks = TimeSpan.FromMilliseconds(50).Ticks;
    }

    public void StartWatching()
    {
        fileSystemWatcher.EnableRaisingEvents = true;
    }

    public void Dispose()
    {
        fileSystemWatcher.Dispose();
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
            return;

        string normalizedPath = e.FullPath.ToUpperInvariant();
        bool hasBeenCached = lastFileChangeTime.TryGetValue(normalizedPath, out long lastChangeTime);

        long ticksNow = DateTime.Now.Ticks;
        if (hasBeenCached && ticksNow - lastChangeTime < updateRateTicks)
            return;

        lastFileChangeTime[normalizedPath] = ticksNow;

        bool isFile = File.Exists(e.FullPath);
        if (isFile)
            FileContentChanged?.Invoke(NormalizeIndex(e.Name!));
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        bool isFile = File.Exists(e.FullPath);
        if (isFile)
            AddIndex(e.FullPath);
        else
            AddDirectoryIndices(e.FullPath);
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        bool isFile = File.Exists(e.FullPath);
        if (isFile)
            RemoveIndex(e.FullPath);
        else
            RemoveCollectionIndex(e.Name!);

        _ = lastFileChangeTime.Remove(e.FullPath.ToUpperInvariant());
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        bool isFile = File.Exists(e.FullPath);
        if (isFile)
        {
            UpdateIndex(e.OldFullPath, e.FullPath);
            IndexChanged?.Invoke(NormalizeIndex(e.OldName!), NormalizeIndex(e.Name!));
        }
        else
        {
            UpdateCollectionIndex(e.OldName!, e.Name!);
            IndexCollectionChanged?.Invoke(NormalizeIndex(e.OldName!), NormalizeIndex(e.Name!));
        }


        string normalizedPath = e.FullPath.ToUpperInvariant();
        if (lastFileChangeTime.Remove(normalizedPath, out long lastChangeTime))
            lastFileChangeTime[normalizedPath] = lastChangeTime;
    }
}
