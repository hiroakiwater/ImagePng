using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ImagePng
{
    public struct ColorRGB
    {
        public byte R;
        public byte G;
        public byte B;

        public ColorRGB(byte r, byte g, byte b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }
    }
    struct Chunk
    {
        public uint Length;
        public byte[] ChunkType;
        public byte[] ChunkData;
        public uint Crc;

        public Chunk(uint length, byte[] chunkType, byte[] chunkData, uint crc)
        {
            Length = length;
            ChunkType = chunkType;
            ChunkData = chunkData;
            Crc = crc;
        }

        public string GetChunkType()
        {
            return Encoding.ASCII.GetString(ChunkType);
        }

        public bool IsIHDR
        {
            get
            {
                if (Encoding.ASCII.GetString(ChunkType) == "IHDR")
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsPLTE // Palette
        {
            get
            {
                if (Encoding.ASCII.GetString(ChunkType) == "PLTE")
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsIDAT
        {
            get
            {
                if (Encoding.ASCII.GetString(ChunkType) == "IDAT")
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsIEND
        {
            get
            {
                if (Encoding.ASCII.GetString(ChunkType) == "IEND" )
                {
                    return true;
                }
                return false;
            }
        }
    }

    public class ImagePng
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public int BitDepth { get; set; }
        public int ColorType { get; set; }
        public int CompressionType { get; set; }
        public int FilterMethod { get; set; }
        public int InterlaceMethod { get; set; }

        List<ColorRGB> pixelsRGB;

        public ImagePng(string fileName)
        {
            pixelsRGB = new List<ColorRGB>();
            Load(fileName);
        }

        public bool Load(string fileName)
        {
            BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open));

            if (ReadPngSignature(reader) == false)
            {
                return false;
            }

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                Chunk chunk = ReadChunk(reader);
                if (chunk.IsIHDR)
                {
                    ReadIHDR(chunk);
                }
                else if (chunk.IsPLTE)
                {
                    ReadPLTE(chunk);
                }
                else if (chunk.IsIDAT)
                {
                    ReadIDAT(chunk);
                } else if (chunk.IsIEND)
                {
                    ReadIEND(chunk);
                }

                //Console.WriteLine("[{0}]", chunk.GetChunkType());
            }

            reader.Close();

            return true;
        }

        private bool ReadPngSignature(BinaryReader reader)
        {
            byte[] signature = reader.ReadBytes(8);

            if (signature[0] == 137 && signature[1] == 80 && signature[2] == 78 && signature[3] == 71 &&
                signature[4] == 13 && signature[5] == 10 && signature[6] == 26 && signature[7] == 10)
            {
                return true;
            }

            return false;
        }

        private Chunk ReadChunk(BinaryReader reader)
        {
            byte[] bytesLength = reader.ReadBytes(4);
            byte[] bytesChunkType = reader.ReadBytes(4);

            uint length = BitConverter.ToUInt32(bytesLength.Reverse().ToArray());

            byte[] chunkData = reader.ReadBytes((int)length);

            byte[] bytesCrc = reader.ReadBytes(4);
            uint crc = BitConverter.ToUInt32(bytesCrc.Reverse().ToArray());

            return new Chunk(length, bytesChunkType, chunkData, crc);
        }

        private void ReadIHDR(Chunk chunk)
        {
            MemoryStream stream = new MemoryStream(chunk.ChunkData);
            BinaryReader reader = new BinaryReader(stream);

            byte[] bytesWidth = reader.ReadBytes(4);
            this.Width = (int)BitConverter.ToUInt32(bytesWidth.Reverse().ToArray());

            byte[] bytesHeight = reader.ReadBytes(4);
            this.Height = (int)BitConverter.ToUInt32(bytesHeight.Reverse().ToArray());

            this.BitDepth = (int)reader.ReadByte();
            this.ColorType = (int)reader.ReadByte();
            this.CompressionType = (int)reader.ReadByte();
            this.FilterMethod = (int)reader.ReadByte();
            this.InterlaceMethod = (int)reader.ReadByte();


            reader.Close();
            stream.Close();
        }

        private void ReadPLTE(Chunk chunk)
        {
            Console.WriteLine("PLTE");
        }

        private void ReadIDAT(Chunk chunk)
        {
            byte[] data = chunk.ChunkData.Take(chunk.ChunkData.Length).ToArray();
            data = data.Skip(2).ToArray();

            byte[] decompressed = null;
            using (MemoryStream stream = new MemoryStream(data))
            {
                stream.Position = 0;
                var decompress = new DeflateStream(stream, CompressionMode.Decompress);

                MemoryStream reader = new MemoryStream();
                decompress.CopyTo(reader);

                decompressed = reader.ToArray();

                decompress.Close();
                reader.Close();
            }
      
            BinaryReader pixels = new BinaryReader(new MemoryStream(decompressed));

            if (BitDepth == 8 && ColorType == 6 && CompressionType == 0)
            {
                for (int y = 0; y < Height; y++)
                {
                    byte line = pixels.ReadByte();
                    ColorRGB previousColor = new ColorRGB(0, 0, 0);

                    for (int x = 0; x < Width; x++)
                    {
                        byte R = pixels.ReadByte();
                        byte G = pixels.ReadByte();
                        byte B = pixels.ReadByte();
                        byte A = pixels.ReadByte();

                        ColorRGB color = new ColorRGB((byte)(previousColor.R + R), (byte)(previousColor.G +  G), (byte)(previousColor.B + B));
                        pixelsRGB.Add(color);

                        previousColor = color;
                    }
                }
            }

            pixels.Close();
        }

        private void ReadIEND(Chunk chunk)
        {
            Console.WriteLine("IEND");
        }

        public ColorRGB GetPixel(int x, int y)
        {
            return pixelsRGB[y * Width + x];
        }
    }
}
