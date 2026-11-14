using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Writes indexed frames to an animated GIF (GIF89a) file.
    /// Each frame after the first stores only the rectangle that changed,
    /// with unchanged pixels inside it marked transparent, which keeps files
    /// small when most of the scene is still.
    /// </summary>
    public static class GifWriter
    {
        private const int LzwMinimumCodeSize = 8;
        private const int MaxLzwCode = 4096;

        /// <summary>
        /// Encodes the frames as a complete GIF file in memory. Returning the
        /// bytes lets a size estimate be saved later without encoding again.
        /// </summary>
        /// <param name="indexedFrames">Top-down palette indices, one array per frame.</param>
        /// <param name="delays">Display time of each frame, in hundredths of a second.</param>
        /// <param name="progress">Optional callback receiving 0-1 progress.</param>
        public static byte[] Encode(
            int width,
            int height,
            GifPalette palette,
            IReadOnlyList<byte[]> indexedFrames,
            IReadOnlyList<int> delays,
            bool loop,
            Action<float> progress = null)
        {
            List<FrameRecord> records = BuildFrameRecords(width, height, indexedFrames, delays);

            using MemoryStream stream = new MemoryStream();

            WriteHeader(stream, width, height, palette, loop);

            for (int i = 0; i < records.Count; i++)
            {
                WriteFrame(stream, records[i]);
                progress?.Invoke((i + 1) / (float)records.Count);
            }

            stream.WriteByte(0x3B);

            return stream.ToArray();
        }

        /// <summary>
        /// Crops each frame to the area that differs from the previous frame.
        /// A frame with no changes is dropped and its time added to the frame before.
        /// </summary>
        private static List<FrameRecord> BuildFrameRecords(
            int width,
            int height,
            IReadOnlyList<byte[]> frames,
            IReadOnlyList<int> delays)
        {
            List<FrameRecord> records = new List<FrameRecord>();
            byte[] previous = null;

            for (int i = 0; i < frames.Count; i++)
            {
                byte[] current = frames[i];

                if (previous == null)
                {
                    records.Add(new FrameRecord(0, 0, width, height, current, delays[i], false));
                    previous = current;
                    continue;
                }

                if (!TryFindChangedBounds(previous, current, width, height,
                        out int left, out int top, out int right, out int bottom))
                {
                    records[records.Count - 1].Delay += delays[i];
                    continue;
                }

                int cropWidth = right - left + 1;
                int cropHeight = bottom - top + 1;
                byte[] pixels = new byte[cropWidth * cropHeight];

                for (int y = 0; y < cropHeight; y++)
                {
                    int row = (top + y) * width + left;

                    for (int x = 0; x < cropWidth; x++)
                    {
                        byte value = current[row + x];

                        // Disposal method 1 leaves the previous frame on screen,
                        // so a transparent pixel shows what was already there.
                        pixels[y * cropWidth + x] = value == previous[row + x]
                            ? (byte)GifPalette.TransparentIndex
                            : value;
                    }
                }

                records.Add(new FrameRecord(left, top, cropWidth, cropHeight, pixels, delays[i], true));
                previous = current;
            }

            return records;
        }

        private static bool TryFindChangedBounds(
            byte[] previous,
            byte[] current,
            int width,
            int height,
            out int left,
            out int top,
            out int right,
            out int bottom)
        {
            left = width;
            top = height;
            right = -1;
            bottom = -1;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;

                for (int x = 0; x < width; x++)
                {
                    if (previous[row + x] == current[row + x])
                    {
                        continue;
                    }

                    if (x < left) left = x;
                    if (x > right) right = x;
                    if (y < top) top = y;
                    bottom = y;
                }
            }

            return right >= 0;
        }

        private static void WriteHeader(Stream stream, int width, int height, GifPalette palette, bool loop)
        {
            WriteAscii(stream, "GIF89a");

            // Logical screen descriptor: global colour table present,
            // 8 bits per channel, 256 entries.
            WriteUInt16(stream, width);
            WriteUInt16(stream, height);
            stream.WriteByte(0xF7);
            stream.WriteByte(0);
            stream.WriteByte(0);

            foreach (Color32 colour in palette.Colours)
            {
                stream.WriteByte(colour.r);
                stream.WriteByte(colour.g);
                stream.WriteByte(colour.b);
            }

            if (!loop)
            {
                return;
            }

            // NETSCAPE2.0 application extension with a loop count of 0 (forever).
            stream.WriteByte(0x21);
            stream.WriteByte(0xFF);
            stream.WriteByte(11);
            WriteAscii(stream, "NETSCAPE2.0");
            stream.WriteByte(3);
            stream.WriteByte(1);
            WriteUInt16(stream, 0);
            stream.WriteByte(0);
        }

        private static void WriteFrame(Stream stream, FrameRecord record)
        {
            // Graphic control extension: disposal method 1 (keep previous frame),
            // optional transparency, and the frame delay.
            stream.WriteByte(0x21);
            stream.WriteByte(0xF9);
            stream.WriteByte(4);
            stream.WriteByte((byte)((1 << 2) | (record.UsesTransparency ? 1 : 0)));
            WriteUInt16(stream, Math.Min(record.Delay, ushort.MaxValue));
            stream.WriteByte(GifPalette.TransparentIndex);
            stream.WriteByte(0);

            // Image descriptor, using the global colour table.
            stream.WriteByte(0x2C);
            WriteUInt16(stream, record.Left);
            WriteUInt16(stream, record.Top);
            WriteUInt16(stream, record.Width);
            WriteUInt16(stream, record.Height);
            stream.WriteByte(0);

            WriteLzw(stream, record.Pixels);
        }

        /// <summary>
        /// Compresses palette indices with GIF's variable-length LZW.
        /// Each new sequence of indices gets the next code; the code size
        /// grows from 9 to 12 bits, and the table is cleared when full.
        /// </summary>
        private static void WriteLzw(Stream stream, byte[] indices)
        {
            int clearCode = 1 << LzwMinimumCodeSize;
            int endCode = clearCode + 1;

            stream.WriteByte(LzwMinimumCodeSize);

            SubBlockBitWriter bits = new SubBlockBitWriter(stream);
            Dictionary<int, int> table = new Dictionary<int, int>(MaxLzwCode);

            int codeSize = LzwMinimumCodeSize + 1;
            int nextCode = endCode + 1;

            bits.Write(clearCode, codeSize);

            int current = indices[0];

            for (int i = 1; i < indices.Length; i++)
            {
                int index = indices[i];
                int key = (current << 8) | index;

                if (table.TryGetValue(key, out int code))
                {
                    current = code;
                    continue;
                }

                bits.Write(current, codeSize);

                if (nextCode == MaxLzwCode)
                {
                    bits.Write(clearCode, codeSize);
                    table.Clear();
                    codeSize = LzwMinimumCodeSize + 1;
                    nextCode = endCode + 1;
                }
                else
                {
                    // Grow the code size just before adding the first code
                    // that does not fit, matching when the decoder grows it.
                    if (nextCode >= 1 << codeSize)
                    {
                        codeSize++;
                    }

                    table[key] = nextCode++;
                }

                current = index;
            }

            bits.Write(current, codeSize);
            bits.Write(endCode, codeSize);
            bits.Flush();

            stream.WriteByte(0);
        }

        private static void WriteUInt16(Stream stream, int value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
        }

        private static void WriteAscii(Stream stream, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// One frame as written to the file: its position, pixels and delay.
        /// </summary>
        private sealed class FrameRecord
        {
            public readonly int Left;
            public readonly int Top;
            public readonly int Width;
            public readonly int Height;
            public readonly byte[] Pixels;
            public readonly bool UsesTransparency;
            public int Delay;

            public FrameRecord(int left, int top, int width, int height, byte[] pixels, int delay, bool usesTransparency)
            {
                Left = left;
                Top = top;
                Width = width;
                Height = height;
                Pixels = pixels;
                Delay = delay;
                UsesTransparency = usesTransparency;
            }
        }

        /// <summary>
        /// Packs codes least-significant bit first and writes them in the
        /// length-prefixed blocks of up to 255 bytes that GIF requires.
        /// </summary>
        private sealed class SubBlockBitWriter
        {
            private readonly Stream stream;
            private readonly byte[] block = new byte[255];
            private int blockLength;
            private int bitBuffer;
            private int bitCount;

            public SubBlockBitWriter(Stream stream)
            {
                this.stream = stream;
            }

            public void Write(int code, int size)
            {
                bitBuffer |= code << bitCount;
                bitCount += size;

                while (bitCount >= 8)
                {
                    AddByte((byte)(bitBuffer & 0xFF));
                    bitBuffer >>= 8;
                    bitCount -= 8;
                }
            }

            public void Flush()
            {
                if (bitCount > 0)
                {
                    AddByte((byte)(bitBuffer & 0xFF));
                    bitBuffer = 0;
                    bitCount = 0;
                }

                if (blockLength > 0)
                {
                    stream.WriteByte((byte)blockLength);
                    stream.Write(block, 0, blockLength);
                    blockLength = 0;
                }
            }

            private void AddByte(byte value)
            {
                block[blockLength++] = value;

                if (blockLength == block.Length)
                {
                    stream.WriteByte((byte)blockLength);
                    stream.Write(block, 0, blockLength);
                    blockLength = 0;
                }
            }
        }
    }
}
