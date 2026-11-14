using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AudioPreviewAttribute))]
public class AudioPreviewDrawer : PropertyDrawer
{
    private const float ButtonWidth = 24f;
    private const float Gap = 2f;

    private static AudioClip currentClip;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            EditorGUI.LabelField(position, label.text, "[AudioPreview] requires an AudioClip field");
            return;
        }

        var fieldRect = new Rect(position.x, position.y, position.width - ButtonWidth - Gap, position.height);
        var buttonRect = new Rect(fieldRect.xMax + Gap, position.y, ButtonWidth, position.height);

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.ObjectField(fieldRect, property, typeof(AudioClip), label);
        EditorGUI.EndProperty();

        var clip = property.objectReferenceValue as AudioClip;
        bool playingThis = clip != null && clip == currentClip && AudioUtil.IsPlaying();

        using (new EditorGUI.DisabledScope(clip == null))
        {
            var icon = EditorGUIUtility.IconContent(playingThis ? "PauseButton" : "PlayButton");
            icon.tooltip = clip == null
                ? "No clip assigned"
                : $"{clip.name}  {clip.length:0.00}s  {clip.frequency} Hz  {clip.channels} ch";

            if (GUI.Button(buttonRect, icon, EditorStyles.miniButton))
            {
                AudioUtil.StopAll();

                if (playingThis)
                {
                    currentClip = null;
                }
                else
                {
                    currentClip = clip;
                    AudioUtil.Play(clip, ((AudioPreviewAttribute)attribute).Loop);
                }
            }
        }
    }

    /// <summary>
    /// Thin wrapper over UnityEditor.AudioUtil (internal), resolved by reflection.
    /// Handles the rename in 2020.2 (PlayClip -> PlayPreviewClip etc.).
    /// </summary>
    private static class AudioUtil
    {
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type Type =
            typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");

        private static readonly MethodInfo PlayMethod =
            Find(new[] { "PlayPreviewClip", "PlayClip" },
                 new[] { typeof(AudioClip), typeof(int), typeof(bool) });

        private static readonly MethodInfo StopAllMethod =
            Find(new[] { "StopAllPreviewClips", "StopAllClips" }, Type.EmptyTypes);

        private static readonly MethodInfo IsPlayingMethod =
            Find(new[] { "IsPreviewClipPlaying" }, Type.EmptyTypes);

        public static void Play(AudioClip clip, bool loop) =>
            PlayMethod?.Invoke(null, new object[] { clip, 0, loop });

        public static void StopAll() =>
            StopAllMethod?.Invoke(null, null);

        // Older editors lack a parameterless query; fall back to tracking state ourselves.
        public static bool IsPlaying() =>
            IsPlayingMethod == null || (bool)IsPlayingMethod.Invoke(null, null);

        private static MethodInfo Find(string[] names, Type[] args)
        {
            if (Type == null) return null;
            foreach (var name in names)
            {
                var m = Type.GetMethod(name, Flags, null, args, null);
                if (m != null) return m;
            }
            Debug.LogWarning($"AudioPreviewDrawer: could not resolve AudioUtil.{names[0]}");
            return null;
        }
    }
}
