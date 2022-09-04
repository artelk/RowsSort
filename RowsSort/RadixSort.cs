using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal unsafe struct Row
{
    private readonly byte* pointer;

    public Row(byte* pointer)
    {
        this.pointer = pointer;
    }

    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => index < (*pointer) ? *(pointer + index + 1) : (byte)0;
    }

    public ulong Num
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var span = new ReadOnlySpan<byte>(pointer + (*pointer) + 1, 8);
            return MemoryMarshal.Read<ulong>(span);
        }
    }

    public Span<byte> Text => new Span<byte>(pointer + 1, *pointer);
}

internal static class RadixSort
{
    [ThreadStatic]
    private static int[] _counts;

    public static void RadixMsdSort(this Span<Row> rows)
    {
        RadixMsdSort(rows, 0);
    }

    private static int[] GetCountsArray()
    {
        var counts = _counts;
        if (counts == null) _counts = counts = new int[256];
        counts.AsSpan().Clear();
        return counts;
    }

    private static void RadixMsdSort(Span<Row> rows, int index)
    {
        int[] counts = GetCountsArray();
        foreach (var r in rows)
            counts[r[index]]++;

        var binStarts = new int[257];
        var binEnds = new int[256];
        binEnds[0] = 0;
        for (int i = 1; i < 256; i++)
            binEnds[i] = binEnds[i - 1] + counts[i - 1];
        binEnds.AsSpan().CopyTo(binStarts);

        int nextBin = 1;
        for (int i = 0; i < rows.Length;)
        {
            byte symbol;
            var r = rows[i];
            while (binEnds[symbol = r[index]] != i)
            {
                var temp = rows[binEnds[symbol]];
                rows[binEnds[symbol]++] = r;
                r = temp;
            }
            rows[i] = r;
            binEnds[symbol]++;
            while (binEnds[nextBin - 1] == binStarts[nextBin]) nextBin++;
            i = binEnds[nextBin - 1];
        }

        index++;
        var slice = rows.Slice(binStarts[0], binEnds[0] - binStarts[0]);
        slice.Sort(new RowComparerSameStrings());
        for (int i = 1; i < 256; i++)
        {
            slice = rows.Slice(binStarts[i], binEnds[i] - binStarts[i]);
            if (slice.Length <= 2 * 1024)
                slice.Sort(new RowComparer(index));
            else
                RadixMsdSort(slice, index);
        }
    }

    private readonly struct RowComparer : IComparer<Row>
    {
        private readonly int startIndex;

        public RowComparer(int startIndex)
        {
            this.startIndex = startIndex;
        }

        public int Compare(Row x, Row y)
        {
            int i = startIndex;
            while (true)
            {
                var cmp = x[i].CompareTo(y[i]);
                if (cmp != 0)
                    return cmp;
                if (x[i] == 0) // && y[i] == 0
                    break;
                i++;
            }
            return x.Num.CompareTo(y.Num);
        }
    }

    private readonly struct RowComparerSameStrings : IComparer<Row>
    {
        public int Compare(Row x, Row y) => x.Num.CompareTo(y.Num);
    }
}
