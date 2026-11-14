using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Captures editor windows and cameras as textures, and places several
    /// captures side by side in one image.
    /// </summary>
    public static class PanelCapture
    {
        /// <summary>
        /// Copies the pixels of an editor window from the screen. The window
        /// must be visible and not covered, because this reads what is on screen.
        /// </summary>
        /// <param name="window">The window to capture.</param>
        /// <param name="cropTopPoints">Points to remove from the top, for example a view's toolbar.</param>
        /// <param name="screenScale">Screen pixels per editor point; above 1 on a scaled display.</param>
        /// <param name="flipVertically">Turns the image over if it comes back upside down.</param>
        public static Texture2D CaptureWindow(EditorWindow window, float cropTopPoints, float screenScale, bool flipVertically)
        {
            Rect area = window.position;
            area.yMin += Mathf.Clamp(cropTopPoints, 0f, area.height - 1f);
            return CaptureScreenArea(area, screenScale, flipVertically);
        }

        /// <summary>
        /// Copies the whole main editor window from the screen, showing every
        /// docked view at once. Floating windows are included only where they
        /// sit over the main window.
        /// </summary>
        public static Texture2D CaptureMainEditorWindow(float screenScale, bool flipVertically)
        {
            return CaptureScreenArea(EditorGUIUtility.GetMainWindowPosition(), screenScale, flipVertically);
        }

        /// <summary>
        /// Copies an area of the screen, given in editor points.
        /// </summary>
        public static Texture2D CaptureScreenArea(Rect area, float screenScale, bool flipVertically)
        {

            Vector2 pixelPosition = new(area.x * screenScale, area.y * screenScale);
            int width = Mathf.Max(1, Mathf.RoundToInt(area.width * screenScale));
            int height = Mathf.Max(1, Mathf.RoundToInt(area.height * screenScale));

            Color[] pixels = InternalEditorUtility.ReadScreenPixel(pixelPosition, width, height);

            if (flipVertically)
                pixels = Flip(pixels, width, height);

            Texture2D texture = new(width, height, TextureFormat.RGB24, false);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Renders a camera at an exact size. Screen Space - Overlay canvases
        /// are not drawn by a camera, so capture the Game view window for those.
        /// </summary>
        public static Texture2D RenderCamera(Camera camera, int width, int height)
        {
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            Texture2D texture = new(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                texture.Apply();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }

            return texture;
        }

        /// <summary>
        /// Places panels left to right, aligned to the top, with a gap between
        /// them. A shorter panel leaves background below it.
        /// </summary>
        public static Texture2D SideBySide(IReadOnlyList<Texture2D> panels, int gap, Color32 background)
        {
            int width = gap * (panels.Count - 1);
            int height = 0;

            foreach (Texture2D panel in panels)
            {
                width += panel.width;
                height = Mathf.Max(height, panel.height);
            }

            Texture2D result = new(width, height, TextureFormat.RGB24, false);

            Color32[] fill = new Color32[width * height];

            for (int i = 0; i < fill.Length; i++)
                fill[i] = background;

            result.SetPixels32(fill);

            int x = 0;

            foreach (Texture2D panel in panels)
            {
                // Texture rows run bottom to top, so a top-aligned panel starts higher up.
                result.SetPixels32(x, height - panel.height, panel.width, panel.height, panel.GetPixels32());
                x += panel.width + gap;
            }

            result.Apply();
            return result;
        }

        private static Color[] Flip(Color[] pixels, int width, int height)
        {
            Color[] flipped = new Color[pixels.Length];

            for (int row = 0; row < height; row++)
                System.Array.Copy(pixels, row * width, flipped, (height - 1 - row) * width, width);

            return flipped;
        }
    }
}
