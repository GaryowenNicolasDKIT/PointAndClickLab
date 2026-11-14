using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GD
{
    /// <summary>
    /// Which movement inputs are held at the moment a frame is captured.
    /// </summary>
    public struct KeyState
    {
        public bool Up;
        public bool Left;
        public bool Down;
        public bool Right;
        public bool Jump;
        public bool Sprint;
    }

    /// <summary>
    /// Draws a WASD, Shift and Space key display into captured frames, so a
    /// GIF shows which inputs produced the movement. Keys are drawn on the
    /// CPU with a built-in 5 x 7 pixel font, so no font asset is needed.
    /// </summary>
    public static class KeyDisplay
    {
        private const int GlyphWidth = 5;
        private const int GlyphHeight = 7;
        private const float StickThreshold = 0.5f;

        private static readonly Color32 ReleasedFill = new Color32(0, 0, 0, 150);
        private static readonly Color32 ReleasedText = new Color32(210, 210, 210, 255);
        private static readonly Color32 PressedFill = new Color32(255, 255, 255, 240);
        private static readonly Color32 PressedText = new Color32(25, 25, 25, 255);

        private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['W'] = new[] { "X...X", "X...X", "X...X", "X.X.X", "X.X.X", "XX.XX", "X...X" },
            ['A'] = new[] { ".XXX.", "X...X", "X...X", "XXXXX", "X...X", "X...X", "X...X" },
            ['S'] = new[] { ".XXXX", "X....", "X....", ".XXX.", "....X", "....X", "XXXX." },
            ['D'] = new[] { "XXXX.", "X...X", "X...X", "X...X", "X...X", "X...X", "XXXX." },
            ['P'] = new[] { "XXXX.", "X...X", "X...X", "XXXX.", "X....", "X....", "X...." },
            ['C'] = new[] { ".XXX.", "X...X", "X....", "X....", "X....", "X...X", ".XXX." },
            ['E'] = new[] { "XXXXX", "X....", "X....", "XXXX.", "X....", "X....", "XXXXX" },
            ['H'] = new[] { "X...X", "X...X", "X...X", "XXXXX", "X...X", "X...X", "X...X" },
            ['I'] = new[] { ".XXX.", "..X..", "..X..", "..X..", "..X..", "..X..", ".XXX." },
            ['F'] = new[] { "XXXXX", "X....", "X....", "XXXX.", "X....", "X....", "X...." },
            ['T'] = new[] { "XXXXX", "..X..", "..X..", "..X..", "..X..", "..X..", "..X.." }
        };

        /// <summary>
        /// Reads the keyboard and the first gamepad. Arrow keys and the left
        /// stick or d-pad light the matching WASD key.
        /// </summary>
        public static KeyState Read()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;

            Vector2 stick = gamepad != null
                ? gamepad.leftStick.ReadValue() + gamepad.dpad.ReadValue()
                : Vector2.zero;

            bool Held(params Key[] keys)
            {
                if (keyboard == null)
                {
                    return false;
                }

                foreach (Key key in keys)
                {
                    if (keyboard[key].isPressed)
                    {
                        return true;
                    }
                }

                return false;
            }

            return new KeyState
            {
                Up = Held(Key.W, Key.UpArrow) || stick.y > StickThreshold,
                Left = Held(Key.A, Key.LeftArrow) || stick.x < -StickThreshold,
                Down = Held(Key.S, Key.DownArrow) || stick.y < -StickThreshold,
                Right = Held(Key.D, Key.RightArrow) || stick.x > StickThreshold,
                Jump = Held(Key.Space) || (gamepad != null && gamepad.buttonSouth.isPressed),
                Sprint = Held(Key.LeftShift, Key.RightShift) || (gamepad != null && gamepad.leftStickButton.isPressed)
            };
        }

        /// <summary>
        /// Draws the key display into a bottom-up Unity pixel array.
        /// </summary>
        public static void Draw(Color32[] pixels, int width, int height, KeyState state, OverlayPosition position)
        {
            // Key size scales with the frame, so the display reads the same at any resolution.
            // The minimum of 22 is the smallest size at which SHIFT and SPACE still fit.
            int unit = Mathf.Max(22, height / 13);
            int gap = Mathf.Max(2, unit / 8);
            int margin = Mathf.Max(6, unit / 3);

            int clusterWidth = unit * 3 + gap * 2;
            int clusterHeight = unit * 3 + gap * 2;
            int halfWidth = (clusterWidth - gap) / 2;

            int column = (int)position % 3;
            int row = (int)position / 3;

            int left = column == 0 ? margin : column == 1 ? (width - clusterWidth) / 2 : width - clusterWidth - margin;
            int top = row == 0 ? margin : row == 1 ? (height - clusterHeight) / 2 : height - clusterHeight - margin;

            PixelCanvas canvas = new PixelCanvas(pixels, width, height);

            canvas.Key(left + unit + gap, top, unit, unit, "W", state.Up);
            canvas.Key(left, top + unit + gap, unit, unit, "A", state.Left);
            canvas.Key(left + unit + gap, top + unit + gap, unit, unit, "S", state.Down);
            canvas.Key(left + (unit + gap) * 2, top + unit + gap, unit, unit, "D", state.Right);
            canvas.Key(left, top + (unit + gap) * 2, halfWidth, unit, "SHIFT", state.Sprint);
            canvas.Key(left + halfWidth + gap, top + (unit + gap) * 2, clusterWidth - halfWidth - gap, unit, "SPACE", state.Jump);
        }

        /// <summary>
        /// Draws into a bottom-up pixel array using top-down coordinates,
        /// clipping anything outside the frame.
        /// </summary>
        private readonly struct PixelCanvas
        {
            private readonly Color32[] pixels;
            private readonly int width;
            private readonly int height;

            public PixelCanvas(Color32[] pixels, int width, int height)
            {
                this.pixels = pixels;
                this.width = width;
                this.height = height;
            }

            public void Key(int x, int y, int keyWidth, int keyHeight, string label, bool pressed)
            {
                FillRect(x, y, keyWidth, keyHeight, pressed ? PressedFill : ReleasedFill);

                // Largest whole-number glyph scale that fits inside the key with
                // a little padding. Labels that cannot fit are left out.
                int textWidthAtOne = label.Length * (GlyphWidth + 1) - 1;
                int scale = Mathf.Min((keyWidth - 4) / textWidthAtOne, (keyHeight - 4) / GlyphHeight);
                scale = Mathf.Min(scale, Mathf.Max(1, keyHeight / 12));

                if (scale < 1)
                {
                    return;
                }

                int textX = x + (keyWidth - textWidthAtOne * scale) / 2;
                int textY = y + (keyHeight - GlyphHeight * scale) / 2;

                foreach (char character in label)
                {
                    DrawGlyph(textX, textY, scale, character, pressed ? PressedText : ReleasedText);
                    textX += (GlyphWidth + 1) * scale;
                }
            }

            private void DrawGlyph(int x, int y, int scale, char character, Color32 colour)
            {
                if (!Glyphs.TryGetValue(character, out string[] rows))
                {
                    return;
                }

                for (int row = 0; row < GlyphHeight; row++)
                {
                    for (int column = 0; column < GlyphWidth; column++)
                    {
                        if (rows[row][column] == 'X')
                        {
                            FillRect(x + column * scale, y + row * scale, scale, scale, colour);
                        }
                    }
                }
            }

            private void FillRect(int x, int y, int rectWidth, int rectHeight, Color32 colour)
            {
                int alpha = colour.a;
                int inverse = 255 - alpha;

                for (int dy = 0; dy < rectHeight; dy++)
                {
                    int topDownRow = y + dy;

                    if (topDownRow < 0 || topDownRow >= height)
                    {
                        continue;
                    }

                    int rowStart = (height - 1 - topDownRow) * width;

                    for (int dx = 0; dx < rectWidth; dx++)
                    {
                        int column = x + dx;

                        if (column < 0 || column >= width)
                        {
                            continue;
                        }

                        Color32 under = pixels[rowStart + column];

                        pixels[rowStart + column] = new Color32(
                            (byte)((under.r * inverse + colour.r * alpha) / 255),
                            (byte)((under.g * inverse + colour.g * alpha) / 255),
                            (byte)((under.b * inverse + colour.b * alpha) / 255),
                            255);
                    }
                }
            }
        }
    }
}
