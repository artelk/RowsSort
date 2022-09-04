using System.IO.MemoryMappedFiles;

internal sealed unsafe class MemoryMappedFileSpan : IDisposable
{
    private readonly MemoryMappedFile memoryMappedFile;
    private readonly MemoryMappedViewAccessor memoryMappedViewAccessor;
    public readonly byte* pointer;
    private bool disposed;

    public MemoryMappedFileSpan(string path)
    {
        Path = path;
        Size = (int)new FileInfo(path).Length;
        memoryMappedFile = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
        memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0, Size);
        memoryMappedViewAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
    }

    public string Path { get; }

    public int Size { get; }

    public ReadOnlySpan<byte> Span => new ReadOnlySpan<byte>(pointer, Size);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MemoryMappedFileSpan()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            memoryMappedViewAccessor.SafeMemoryMappedViewHandle.ReleasePointer();

            if (disposing)
            {
                memoryMappedViewAccessor.Dispose();
                memoryMappedFile.Dispose();
            }

            disposed = true;
        }
    }
}
