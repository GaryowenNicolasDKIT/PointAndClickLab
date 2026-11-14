using UnityEngine;

/// <summary>
/// Adds a play/stop button beside an AudioClip field in the Inspector.
/// Usage: [AudioPreview] public AudioClip hitSound;
/// </summary>
public class AudioPreviewAttribute : PropertyAttribute
{
    public readonly bool Loop;

    public AudioPreviewAttribute(bool loop = false)
    {
        Loop = loop;
    }
}
