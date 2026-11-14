using System;
using System.Collections.Generic;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Builds one 255-colour palette shared by every frame of a GIF, using
    /// median cut, and maps captured pixels to palette indices.
    /// A shared palette stops colours flickering between frames.
    /// </summary>
    public sealed class GifPalette
    {
        /// <summary>
        /// Index reserved for transparent pixels, used to mark pixels that
        /// did not change since the previous frame.
        /// </summary>
        public const int TransparentIndex = 255;

        private const int MaxColours = 255;
        private const int BinCount = 1 << 15;
        private const float DitherSpread = 16f;

        // 4x4 ordered dither thresholds. Ordered dither is fixed to screen
        // position, so static areas stay identical between frames.
        private static readonly int[] BayerMatrix =
        {
            0, 8, 2, 10,
            12, 4, 14, 6,
            3, 11, 1, 9,
            15, 7, 13, 5
        };

        private readonly byte[] lookup;

        /// <summary>
        /// The 256-entry colour table written to the GIF header.
        /// </summary>
        public Color32[] Colours { get; }

        /// <summary>
        /// Number of palette entries in use (at most 255).
        /// </summary>
        public int ColourCount { get; }

        private GifPalette(Color32[] colours, int colourCount, byte[] lookup)
        {
            Colours = colours;
            ColourCount = colourCount;
            this.lookup = lookup;
        }

        /// <summary>
        /// Builds a palette from every pixel of every captured frame.
        /// </summary>
        public static GifPalette Build(IReadOnlyList<Color32[]> frames)
        {
            // Histogram of colours reduced to 5 bits per channel. The sums keep
            // the full 8-bit values so palette colours are accurate averages.
            long[] counts = new long[BinCount];
            long[] sumR = new long[BinCount];
            long[] sumG = new long[BinCount];
            long[] sumB = new long[BinCount];

            foreach (Color32[] frame in frames)
            {
                foreach (Color32 colour in frame)
                {
                    int bin = ToBin(colour.r, colour.g, colour.b);
                    counts[bin]++;
                    sumR[bin] += colour.r;
                    sumG[bin] += colour.g;
                    sumB[bin] += colour.b;
                }
            }

            List<int> usedBins = new List<int>();

            for (int bin = 0; bin < BinCount; bin++)
            {
                if (counts[bin] > 0)
                {
                    usedBins.Add(bin);
                }
            }

            int[] bins = usedBins.ToArray();
            List<Box> boxes = MedianCut(bins, counts);

            Color32[] colours = new Color32[256];

            for (int i = 0; i < boxes.Count; i++)
            {
                long total = 0;
                long r = 0;
                long g = 0;
                long b = 0;

                for (int j = boxes[i].Start; j < boxes[i].End; j++)
                {
                    int bin = bins[j];
                    total += counts[bin];
                    r += sumR[bin];
                    g += sumG[bin];
                    b += sumB[bin];
                }

                colours[i] = new Color32(
                    (byte)(r / total),
                    (byte)(g / total),
                    (byte)(b / total),
                    255);
            }

            return new GifPalette(
                colours,
                boxes.Count,
                BuildLookup(colours, boxes.Count));
        }

        /// <summary>
        /// Converts a bottom-up Unity pixel array into top-down palette
        /// indices, the row order GIF expects.
        /// </summary>
        public byte[] ToIndices(Color32[] pixels, int width, int height, bool dither)
        {
            byte[] indices = new byte[width * height];

            for (int y = 0; y < height; y++)
            {
                int source = (height - 1 - y) * width;
                int destination = y * width;

                for (int x = 0; x < width; x++)
                {
                    Color32 colour = pixels[source + x];

                    if (!dither)
                    {
                        indices[destination + x] =
                            lookup[ToBin(colour.r, colour.g, colour.b)];
                        continue;
                    }

                    // Centre the threshold on zero so dithering does not
                    // brighten or darken the image overall.
                    float offset =
                        (BayerMatrix[(y & 3) * 4 + (x & 3)] / 16f - 0.46875f) * DitherSpread;

                    indices[destination + x] = lookup[ToBin(
                        ClampToByte(colour.r + offset),
                        ClampToByte(colour.g + offset),
                        ClampToByte(colour.b + offset))];
                }
            }

            return indices;
        }

        /// <summary>
        /// Repeatedly splits the colour box with the widest spread until the
        /// palette is full or no box can be split.
        /// </summary>
        private static List<Box> MedianCut(int[] bins, long[] counts)
        {
            List<Box> boxes = new List<Box> { Box.Create(bins, counts, 0, bins.Length) };
            int[] keys = new int[bins.Length];

            while (boxes.Count < MaxColours)
            {
                int best = -1;
                double bestScore = 0;

                for (int i = 0; i < boxes.Count; i++)
                {
                    if (boxes[i].Length < 2)
                    {
                        continue;
                    }

                    // Weighting by pixel count spends palette entries on the
                    // colours that cover most of the image.
                    double score = boxes[i].LongestRange * Math.Sqrt(boxes[i].PixelCount);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = i;
                    }
                }

                if (best < 0)
                {
                    break;
                }

                Box box = boxes[best];

                for (int i = box.Start; i < box.End; i++)
                {
                    keys[i] = (bins[i] >> box.LongestShift) & 31;
                }

                Array.Sort(keys, bins, box.Start, box.Length);

                // Split at the pixel-weighted median, keeping both halves non-empty.
                long half = box.PixelCount / 2;
                long running = 0;
                int split = box.End - 1;

                for (int i = box.Start; i < box.End - 1; i++)
                {
                    running += counts[bins[i]];

                    if (running >= half)
                    {
                        split = i + 1;
                        break;
                    }
                }

                boxes[best] = Box.Create(bins, counts, box.Start, split);
                boxes.Add(Box.Create(bins, counts, split, box.End));
            }

            return boxes;
        }

        /// <summary>
        /// Precomputes the nearest palette entry for every 15-bit colour, so
        /// mapping a pixel is a single array read.
        /// </summary>
        private static byte[] BuildLookup(Color32[] colours, int colourCount)
        {
            byte[] table = new byte[BinCount];

            for (int bin = 0; bin < BinCount; bin++)
            {
                int r = (((bin >> 10) & 31) << 3) | 4;
                int g = (((bin >> 5) & 31) << 3) | 4;
                int b = ((bin & 31) << 3) | 4;

                int bestIndex = 0;
                int bestDistance = int.MaxValue;

                for (int i = 0; i < colourCount; i++)
                {
                    int dr = r - colours[i].r;
                    int dg = g - colours[i].g;
                    int db = b - colours[i].b;
                    int distance = dr * dr + dg * dg + db * db;

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                table[bin] = (byte)bestIndex;
            }

            return table;
        }

        private static int ToBin(int r, int g, int b)
        {
            return ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);
        }

        private static int ClampToByte(float value)
        {
            return value < 0f ? 0 : value > 255f ? 255 : (int)value;
        }

        /// <summary>
        /// A contiguous range of histogram bins, with the channel it spans most.
        /// </summary>
        private readonly struct Box
        {
            public readonly int Start;
            public readonly int End;
            public readonly long PixelCount;
            public readonly int LongestRange;
            public readonly int LongestShift;

            public int Length => End - Start;

            private Box(int start, int end, long pixelCount, int longestRange, int longestShift)
            {
                Start = start;
                End = end;
                PixelCount = pixelCount;
                LongestRange = longestRange;
                LongestShift = longestShift;
            }

            public static Box Create(int[] bins, long[] counts, int start, int end)
            {
                int minR = 31, minG = 31, minB = 31;
                int maxR = 0, maxG = 0, maxB = 0;
                long pixelCount = 0;

                for (int i = start; i < end; i++)
                {
                    int bin = bins[i];
                    int r = (bin >> 10) & 31;
                    int g = (bin >> 5) & 31;
                    int b = bin & 31;

                    minR = Math.Min(minR, r);
                    maxR = Math.Max(maxR, r);
                    minG = Math.Min(minG, g);
                    maxG = Math.Max(maxG, g);
                    minB = Math.Min(minB, b);
                    maxB = Math.Max(maxB, b);

                    pixelCount += counts[bin];
                }

                int rangeR = maxR - minR;
                int rangeG = maxG - minG;
                int rangeB = maxB - minB;

                if (rangeG >= rangeR && rangeG >= rangeB)
                {
                    return new Box(start, end, pixelCount, rangeG, 5);
                }

                if (rangeR >= rangeB)
                {
                    return new Box(start, end, pixelCount, rangeR, 10);
                }

                return new Box(start, end, pixelCount, rangeB, 0);
            }
        }
    }
}
