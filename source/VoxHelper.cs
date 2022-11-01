// Copyright (C) 2022 Maxim Gumin, The MIT License (MIT)
// Edited by Adam Albanese @Kormaican

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

static class VoxHelper {

    // If there are issues regarding palettes, then possibly look into customPalette in Program.Main
    public static Dictionary<char, int> paletteGlobal = XDocument.Load("resources/palette.xml").Root.Elements("color").ToDictionary(x => x.Get<char>("symbol"), x => (255 << 24) + Convert.ToInt32(x.Get<string>("value"), 16));
    
    // This is swapped from above, so color ints are the index and chars are the values
    public static Dictionary<int, char> paletteGlobalSwapped = XDocument.Load("resources/palette.xml").Root.Elements("color").ToDictionary(x => (255 << 24) + Convert.ToInt32(x.Get<string>("value"), 16), x => x.Get<char>("symbol"));

    public static (int[], int, int, int) LoadVox(string filename) {
        int[] result = null;
        
        try {
            using FileStream file = File.Open(filename, FileMode.Open);
            var stream = new BinaryReader(file);

            int mX = -1, mY = -1, mZ = -1;

            string magic = new(stream.ReadChars(4));
            int version = stream.ReadInt32();
            
            while (stream.BaseStream.Position < stream.BaseStream.Length) {
                string tag = Encoding.ASCII.GetString(stream.ReadBytes(4));
                
                int chunkBytes = stream.ReadInt32();
                int childBytes = stream.ReadInt32();

                if (tag.Equals("MAIN"))
                    continue;
                if (tag.Equals("PACK")) {
                    int modelCount = stream.ReadInt32();
                } else if (tag.Equals("SIZE")) {
                    mX = stream.ReadInt32();
                    mY = stream.ReadInt32();
                    mZ = stream.ReadInt32();
                    result = new int[mX * mY * mZ];
                } else if (tag.Equals("XYZI")) {
                    int numVoxels = stream.ReadInt32();
                    for (int i = 0; i < numVoxels; i++) {
                        byte x = stream.ReadByte();
                        byte y = stream.ReadByte();
                        byte z = stream.ReadByte();
                        byte color = stream.ReadByte();
                        result[x + y * mX + z * mX * mY] = color;
                        // Console.WriteLine($"adding voxel {x} {y} {z} of color {color}");
                    }
                } else if (tag.Equals("RGBA")) {
                    int[] palette = new int[256];
                    byte r, g, b;
                    int color;
                    for (int i = 0; i < palette.Length; i++) {
                        r = stream.ReadByte();
                        g = stream.ReadByte();
                        b = stream.ReadByte();
                        color = stream.ReadByte();
                        color |= b << 8;
                        color |= g << 16;
                        color |= r << 24;
                        palette.Append(color);
                    }
                } else {
                    stream.ReadBytes(chunkBytes);
                }
            }
            file.Close();
            return (result, mX, mY, mZ);
        }
        catch (Exception) { return (null, -1, -1, -1); }
    }


    //Added by Adam Albanese @Kormaican 
    // Returns Palette also
    public static (int[], int, int, int, int[]) LoadVox2(string filename)
    {
        int[] result = null;

        try
        {
            using FileStream file = File.Open(filename, FileMode.Open);
            var stream = new BinaryReader(file);

            int mX = -1, mY = -1, mZ = -1;

            string magic = new(stream.ReadChars(4));
            int version = stream.ReadInt32();

            int[] palette_list = new int[256];

            while (stream.BaseStream.Position < stream.BaseStream.Length)
            {
                string tag = Encoding.ASCII.GetString(stream.ReadBytes(4));

                int chunkBytes = stream.ReadInt32();
                int childBytes = stream.ReadInt32();

                

                if (tag.Equals("MAIN"))
                    continue;
                if (tag.Equals("PACK"))
                {
                    int modelCount = stream.ReadInt32();
                }
                else if (tag.Equals("SIZE"))
                {
                    mX = stream.ReadInt32();
                    mY = stream.ReadInt32();
                    mZ = stream.ReadInt32();
                    result = new int[mX * mY * mZ];
                }
                else if (tag.Equals("XYZI"))
                {
                    int numVoxels = stream.ReadInt32();
                    for (int i = 0; i < numVoxels; i++)
                    {
                        byte x = stream.ReadByte();
                        byte y = stream.ReadByte();
                        byte z = stream.ReadByte();
                        byte color = stream.ReadByte();
                        result[x + y * mX + z * mX * mY] = color;
                        // Console.WriteLine($"adding voxel {x} {y} {z} of color {color}");
                    }
                }
                else if (tag.Equals("RGBA"))
                {
                    int[] palette = new int[256];
                    byte r, g, b;
                    int color;
                    for (int i = 0; i < palette.Length; i++)
                    {
                        r = stream.ReadByte();
                        g = stream.ReadByte();
                        b = stream.ReadByte();
                        color = stream.ReadByte();
                        color |= b << 8;
                        color |= g << 16;
                        color |= r << 24;
                        palette.Append(color); // Not sure what this line was for. Maybe it was stale code?
                        palette_list[i] = color;
                    }

                }
                else
                {
                    stream.ReadBytes(chunkBytes);
                }
            }
            file.Close();
            return (result, mX, mY, mZ, palette_list);
        }
        catch (Exception) { return (null, -1, -1, -1, null); }
    }

    static void WriteString(this BinaryWriter stream, string s) { foreach (char c in s) stream.Write(c); }
    public static void SaveVox(byte[] state, byte MX, byte MY, byte MZ, int[] palette, string filename)
    {
        List<(byte, byte, byte, byte)> voxels = new();
        for (byte z = 0; z < MZ; z++) for (byte y = 0; y < MY; y++) for (byte x = 0; x < MX; x++)
                {
                    int i = x + y * MX + z * MX * MY;
                    byte v = state[i];
                    if (v != 0) voxels.Add((x, y, z, (byte)(v + 1)));
                }

        FileStream file = File.Open(filename, FileMode.Create);
        using BinaryWriter stream = new(file);

        stream.WriteString("VOX ");
        stream.Write(150); // Version must always be 150

        stream.WriteString("MAIN");
        stream.Write(0); // Main has no content
        stream.Write(1092 + voxels.Count * 4); // Child Size in Bytes

        stream.WriteString("PACK");
        stream.Write(4); // Size of Int
        stream.Write(0); // No Children
        stream.Write(1); // Only 1 model

        stream.WriteString("SIZE");
        stream.Write(12); // Size of 3 Ints
        stream.Write(0); // No Children
        stream.Write((int)MX); // Model Width
        stream.Write((int)MY); // Model Length
        stream.Write((int)MZ); // Model Height

        stream.WriteString("XYZI");
        stream.Write(4 + voxels.Count * 4); // Size of Voxels and length
        stream.Write(0); // No Children
        stream.Write(voxels.Count); // Amount of voxels / points

        foreach (var (x, y, z, color) in voxels) {
            stream.Write(x); // X Coord
            //stream.Write((byte)(size.y - v.y - 1));
            stream.Write(y); // Y Coord
            stream.Write(z); // Z Coord
            stream.Write(color); // Color Palette Index
        }

        stream.WriteString("RGBA");
        stream.Write(1024); // Always 256 Colors
        stream.Write(0); // No Children

        foreach (int c in palette) {
            //(byte R, byte G, byte B) = c.ToTuple();
            stream.Write((byte)((c & 0xff0000) >> 16)); // r
            stream.Write((byte)((c & 0xff00) >> 8)); // g
            stream.Write((byte)(c & 0xff)); // b
            stream.Write((byte)0); // a
        }
        
        // Fill the palette with blanks
        for (int i = palette.Length; i < 256; i++) {
            stream.Write((byte)(0xff - i - 1));
            stream.Write((byte)(0xff - i - 1));
            stream.Write((byte)(0xff - i - 1));
            stream.Write((byte)(0xff));
        }
        stream.Write(0);
        file.Close();
    }
}
