using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

static class Program
{
    static unsafe void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: RowsSort <file path>");
            return;
        }

        var filePath = Path.GetFullPath(args[0]);
        //var filePath = Path.GetFullPath(@"d:\temp\test");
        var folderName = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(folderName);

        var sw = Stopwatch.StartNew();
        var files = SplitToFiles(filePath, folderName);
        using (var outFile = File.Create("result", 1024 * 1024))
        {
            var rows = new ArrayList<Row>(1024);
            foreach (var (ch, path) in files)
            {
                using var mmfile = new MemoryMappedFileSpan(path);
                rows.Reset();
                var ptr = mmfile.pointer;
                var lastPtr = ptr + mmfile.Size;
                while (ptr != lastPtr)
                {
                    rows.Add(new Row(ptr));
                    ptr += (*ptr) + 9;
                }
                RadixSort.RadixMsdSort(rows.AsSpan());
                Save(outFile, rows.AsSpan(), (byte)ch);
            }
        }
        sw.Stop();
        Console.WriteLine(sw.Elapsed);
        Directory.Delete(folderName, true);
    }

    private static void Save(FileStream outFile, Span<Row> rows, byte ch)
    {
        Span<char> numCharBuffer = stackalloc char[30];
        Span<byte> numBuffer = stackalloc byte[30];
        foreach (var row in rows)
        {
            row.Num.TryFormat(numCharBuffer, out var charsWritten);
            var bytesWritten = Encoding.ASCII.GetBytes(numCharBuffer[..charsWritten], numBuffer);
            outFile.Write(numBuffer[..bytesWritten]);
            outFile.WriteByte((byte)'.');
            outFile.WriteByte((byte)' ');
            outFile.WriteByte(ch);
            outFile.Write(row.Text);
            outFile.WriteByte((byte)'\r');
            outFile.WriteByte((byte)'\n');
        }
    }

    private static List<(char ch, string path)> SplitToFiles(string filePath, string outFolder)
    {
        var filesDict = new Dictionary<(char ch, byte order), (string path, FileStream fs)>();
        Span<byte> buffer8 = stackalloc byte[8];
        Span<byte> buffer256 = stackalloc byte[256];
        var fileNum = 0;
        using (var inFile = File.OpenText(filePath))
        {
            string? line;
            while ((line = inFile.ReadLine()) != null)
            {
                var span = line.AsSpan();
                var i = span.IndexOf('.');
                var n = ulong.Parse(span[..i]);
                span = span[(i + 2)..]; // + skip space
                var ch = span[0];
                var order = (byte)(span[1] >> 5);
                if (!filesDict.TryGetValue((ch, order), out var p))
                {
                    var path = $"{outFolder}/{++fileNum}";
                    var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);
                    p = (path, fs);
                    filesDict.Add((ch, order), p);
                }
                span = span[1..]; // skip 1st char
                var len = Encoding.ASCII.GetBytes(span, buffer256);
                p.fs.WriteByte((byte)len);
                p.fs.Write(buffer256[..len]);
                MemoryMarshal.Write(buffer8, ref n);
                p.fs.Write(buffer8);
            }
        }
        foreach (var p in filesDict.Values)
        {
            p.fs.Close();
            p.fs.Dispose();
        }

        return filesDict.OrderBy(p => (p.Key.ch, p.Key.order)).Select(p => (p.Key.ch, p.Value.path)).ToList();
    }
}
