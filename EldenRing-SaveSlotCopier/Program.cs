using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

internal class Program
{    static async Task Main(string[] args)
    {
        var inputOpt = new Option<FileInfo>("--input", "Input");
        var outputOpt = new Option<FileInfo>("--output", "Output");
        var sourceOpt = new Option<int>("--source", "Source");
        var destOpt = new Option<int>("--dest", "Dest");

        inputOpt.AddAlias("-i");
        outputOpt.AddAlias("-o");
        sourceOpt.AddAlias("-s");
        destOpt.AddAlias("-d");

        var argparser = new RootCommand
        {
            inputOpt,
            outputOpt,
            sourceOpt,
            destOpt
        };

        argparser.Description = "Elden Ring Save Slot Copier";

        argparser.SetHandler((FileInfo input, FileInfo output, int source, int dest) => ValidateThenExecute(input, output, source, dest), inputOpt, outputOpt, sourceOpt, destOpt);

        await argparser.InvokeAsync(args);
    }

    public struct BND4Header
    {
        public byte[] magic = new byte[4];
        public int unk1;
        public int unk2;
        public int fileCount;
        public int unk3;
        public int unk4;
        public byte[] version = new byte[8];
        public int entrySize;
        public int unk5;
        public int dataOffset;
        public int unk6;
        public byte encoding;
        public byte[] unk7 = new byte[15];

        public BND4Header()
        {
            unk1 = 0;
            unk2 = 0;
            fileCount = 0;
            unk3 = 0;
            unk4 = 0;
            version = new byte[8];
            entrySize = 0;
            unk5 = 0;
            dataOffset = 0;
            unk6 = 0;
            encoding = 0;
            unk7 = new byte[8];
        }
    }

    struct BDN4FileEntry
    {
        public int index;
        public int unk1;
        public int unk2;
        public int size;
        public int unk3;
        public int offset;
        public int nameOffset;
        public int unk4;
        public int unk5;
    }

    struct BDN4FileData
    {
        public BDN4FileEntry entry;
        public String name;
        public byte[] data;
    }

    struct BDN4File
    {
        public BND4Header header = new BND4Header();
        public List<BDN4FileEntry> entries = new List<BDN4FileEntry>();
        public List<BDN4FileData> files = new List<BDN4FileData>();

        public BDN4File()
        {
        }
    }

    public static void ValidateThenExecute(FileInfo input, FileInfo output, int source, int dest)
    {
        if (input == null || !input.Exists)
        {
            Console.WriteLine("Error: Input File Does Not Exist");
            return;
        }
        else if (output != null && output.Exists)
        {
            Console.WriteLine("Error: Output File Already Exists");
            return;
        }
        else if (source <= 0 || source > 9)
        {
            Console.WriteLine("Error: Source Slot Must Be 1-9");
            return;
        }
        else if (dest <= 0 || source > 9)
        {
            Console.WriteLine("Error: Destination Slot Must Be 1-9");
            return;
        }
        else if (source == dest)
        {
            Console.WriteLine($"Error: Source Slot Same As Destination Slot {source} {dest}");
            return;
        }

        Console.WriteLine($"Source File: {input} Output {output} Source Slot {source} Destination Slot {dest}");

        Execute(input, output, source, dest);
    }
    public static void Execute(FileInfo input, FileInfo output, int source, int dest)
    {
        int i;
        BDN4File file = new BDN4File();
        FileStream istream = input.OpenRead();
        BinaryReader reader = new BinaryReader(istream, Encoding.Unicode, false);

        file.header.magic = reader.ReadBytes(4);

        if (file.header.magic[0] != 'B' ||
            file.header.magic[1] != 'N' ||
            file.header.magic[2] != 'D' || 
            file.header.magic[3] != '4')
        {
            Console.WriteLine("Error: Invalid File Type");
            return;
        }

        file.header.unk1                = reader.ReadInt32();
        file.header.unk2                = reader.ReadInt32();
        file.header.fileCount           = reader.ReadInt32();
        file.header.unk3                = reader.ReadInt32();
        file.header.unk4                = reader.ReadInt32();
        file.header.version             = reader.ReadBytes(8);
        file.header.entrySize           = reader.ReadInt32();
        file.header.unk5                = reader.ReadInt32();
        file.header.dataOffset          = reader.ReadInt32();
        file.header.unk6                = reader.ReadInt32();
        file.header.encoding            = reader.ReadByte();
        file.header.unk7                = reader.ReadBytes(15);

        if (file.header.encoding < 0 || file.header.encoding > 1)
        {
            Console.WriteLine("Error: Invalid Encoding");
            return;
        }

        Console.Write($"Entry Start Pos: {istream.Position}");

        for (i = 0; i < file.header.fileCount; i++)
        {
            BDN4FileEntry entry = new BDN4FileEntry();

            entry.index = i;
            entry.unk1 = reader.ReadInt32();
            entry.unk2 = reader.ReadInt32();
            entry.size = reader.ReadInt32();
            entry.unk3 = reader.ReadInt32();
            entry.offset = reader.ReadInt32();
            entry.nameOffset = reader.ReadInt32();
            entry.unk4 = reader.ReadInt32();
            entry.unk5 = reader.ReadInt32();
 
            file.entries.Add(entry);
        }

        Console.WriteLine($"Version: {new String(file.header.version.ToString())}");
        Console.WriteLine($"File Count: {file.header.fileCount}");
        Console.WriteLine($"Entry Size: {file.header.entrySize }");
        Console.WriteLine($"Encoding: {file.header.encoding}");
        Console.WriteLine($"Data Start: {file.header.dataOffset}");
        
        foreach (var entry in file.entries)
        {
            BDN4FileData data = new BDN4FileData();

            data.entry = entry;

            if (entry.nameOffset > 0)
            {
                istream.Seek(entry.nameOffset, SeekOrigin.Begin);

                data.name = ReadUnicodeString(reader);
            }

            if (entry.offset > 0 && entry.size > 0)
            {
                istream.Seek(entry.offset, SeekOrigin.Begin);
                data.data = reader.ReadBytes(entry.size);
            }

            file.files.Add(data);
        }

        foreach(var f in file.files)
        {
            Console.WriteLine($"File: [{f.entry.index}] {f.name} Entry Size: {f.entry.size} Offset: {f.entry.offset} Name Offset: {f.entry.nameOffset} Read Size: {f.data.Length}");
        }

        int sourceIndex = source - 1;
        int destIndex = dest - 1;

        BDN4FileData sourceData = file.files[sourceIndex];
        BDN4FileData targetData = file.files[destIndex];
        
        // If output was specified, write a whole new file at the specified location
        // Otherwise, copy slot in place

        if (output != null && output.Name.Length > 0)
        {
            FileStream ostream = output.Create();

            // Copy the source to the destination
            istream.Seek(0, SeekOrigin.Begin);
            istream.CopyTo(ostream);

            ostream.Close();

            // Then copy the source data to the target data segment
            ostream = output.OpenWrite();

            ostream.Seek(targetData.entry.offset, SeekOrigin.Begin);
            ostream.Write(sourceData.data);

            istream.Close();
            ostream.Close();
        }
        else
        {
            istream.Close(); 
            
            FileStream ostream = input.OpenWrite();

            ostream.Seek(targetData.entry.offset, SeekOrigin.Begin);
            ostream.Write(sourceData.data);

            ostream.Close();
        }
    }

    static String ReadUnicodeString(BinaryReader reader)
    {
        byte[] name = new byte[128];
        int bi = 0;

        while (true)
        {
            var l = reader.ReadBytes(2);

            if (l[0] == 0 && l[1] == 0) break;

            name[bi]     = l[0];
            name[bi + 1] = l[1];

            bi += 2;
        }

        return Encoding.Unicode.GetString(name);
    }
}