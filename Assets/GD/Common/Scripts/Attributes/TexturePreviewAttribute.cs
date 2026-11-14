using UnityEngine;

/// <summary>
/// Draws a thumbnail below a Texture or Sprite field in the Inspector.
/// Usage: [TexturePreview(96)] public Texture2D albedo;
/// </summary>
public class TexturePreviewAttribute : PropertyAttribute
{
    public readonly float Size;

    public TexturePreviewAttribute(float size = 64f)
    {
        Size = Mathf.Max(16f, size);
    }
}
