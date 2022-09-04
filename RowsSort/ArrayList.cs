using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal struct ArrayList<T>
{
    private T[] array;
    private int count;
    public ArrayList(int capacity)
    {
        array = new T[capacity];
        count = 0;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count;
    }

    public void Reset() => count = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (array.Length == count)
            Array.Resize(ref array, count << 1);
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), count++) = item;
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        while (count + items.Length > array.Length)
            Array.Resize(ref array, count << 1);
        items.CopyTo(array.AsSpan(count));
        count += items.Length;
    }

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
    }

    public Span<T> AsSpan() => array.AsSpan(0, count);
}
