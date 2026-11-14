using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Provides a searchable browser for ScriptableObject assets in the project.
    /// Assets are grouped by ScriptableObject type and can be selected, pinged,
    /// or opened directly from the browser.
    /// </summary>
    public class ScriptableObjectBrowser : EditorWindow
    {
        private sealed class ScriptableObjectEntry
        {
            public string Name;
            public string Path;
            public Type Type;
            public ScriptableObject Asset;
        }

        private readonly List<ScriptableObjectEntry> entries = new();
        private readonly Dictionary<Type, bool> foldoutStates = new();

        private Vector2 scrollPosition;
        private string searchText = string.Empty;
        private bool showPaths;
        private int assetCount;

        /// <summary>
        /// Opens the ScriptableObject Browser window.
        /// </summary>
        [MenuItem("Tools/GD/Navigate/ScriptableObject Browser", priority = 101)]
        private static void ShowWindow()
        {
            ScriptableObjectBrowser window =
                GetWindow<ScriptableObjectBrowser>();

            window.titleContent =
                new GUIContent("ScriptableObject Browser");

            window.minSize = new Vector2(430f, 320f);
            window.Show();
        }

        /// <summary>
        /// Refreshes the asset list when the window is opened or re-enabled.
        /// </summary>
        private void OnEnable()
        {
            RefreshAssets();
        }

        /// <summary>
        /// Draws the browser interface.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();

            GUILayout.Space(4f);

            DrawAssetList();
        }

        /// <summary>
        /// Draws the heading and asset count.
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "ScriptableObject Browser",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Browse ScriptableObject assets in the project. " +
                "Click an asset to select and highlight it. " +
                "Double-click to open it in the Inspector.",
                MessageType.Info);

            EditorGUILayout.LabelField(
                $"{assetCount} ScriptableObject asset{(assetCount == 1 ? string.Empty : "s")} found.",
                EditorStyles.miniLabel);
        }

        /// <summary>
        /// Draws search, display, and refresh controls.
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            string newSearchText =
                GUILayout.TextField(
                    searchText,
                    GUI.skin.FindStyle("ToolbarSearchTextField"),
                    GUILayout.ExpandWidth(true));

            if (newSearchText != searchText)
            {
                searchText = newSearchText;
                Repaint();
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                if (GUILayout.Button(
                        GUIContent.none,
                        GUI.skin.FindStyle("ToolbarSearchCancelButton")))
                {
                    searchText = string.Empty;
                    GUI.FocusControl(null);
                    Repaint();
                }
            }

            showPaths = GUILayout.Toggle(
                showPaths,
                "Paths",
                EditorStyles.toolbarButton,
                GUILayout.Width(50f));

            if (GUILayout.Button(
                    "Refresh",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(60f)))
            {
                RefreshAssets();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws ScriptableObject assets grouped by concrete type.
        /// </summary>
        private void DrawAssetList()
        {
            List<ScriptableObjectEntry> filteredEntries =
                GetFilteredEntries();

            if (filteredEntries.Count == 0)
            {
                GUILayout.Space(10f);

                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(searchText)
                        ? "No ScriptableObject assets were found in this project."
                        : "No ScriptableObject assets match the current search.",
                    MessageType.None);

                return;
            }

            IEnumerable<IGrouping<Type, ScriptableObjectEntry>> groups =
                filteredEntries
                    .GroupBy(entry => entry.Type)
                    .OrderBy(group => group.Key.Name);

            scrollPosition =
                EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (IGrouping<Type, ScriptableObjectEntry> group in groups)
            {
                DrawTypeGroup(group.Key, group);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws a foldout for one ScriptableObject type.
        /// </summary>
        private void DrawTypeGroup(
            Type type,
            IEnumerable<ScriptableObjectEntry> group)
        {
            if (!foldoutStates.ContainsKey(type))
            {
                foldoutStates[type] = true;
            }

            List<ScriptableObjectEntry> groupEntries =
                group
                    .OrderBy(entry => entry.Name)
                    .ToList();

            string label =
                $"{ObjectNames.NicifyVariableName(type.Name)} ({groupEntries.Count})";

            foldoutStates[type] =
                EditorGUILayout.Foldout(
                    foldoutStates[type],
                    label,
                    true);

            if (!foldoutStates[type])
                return;

            EditorGUI.indentLevel++;

            foreach (ScriptableObjectEntry entry in groupEntries)
            {
                DrawAssetEntry(entry);
            }

            EditorGUI.indentLevel--;

            GUILayout.Space(3f);
        }

        /// <summary>
        /// Draws an individual ScriptableObject asset row.
        /// </summary>
        private void DrawAssetEntry(ScriptableObjectEntry entry)
        {
            Rect rowRect =
                EditorGUILayout.GetControlRect(
                    false,
                    showPaths ? 34f : 20f);

            Rect iconRect =
                new Rect(
                    rowRect.x,
                    rowRect.y + 1f,
                    18f,
                    18f);

            Rect labelRect =
                new Rect(
                    rowRect.x + 22f,
                    rowRect.y,
                    rowRect.width - 22f,
                    18f);

            Texture icon =
                AssetPreview.GetMiniThumbnail(entry.Asset);

            if (icon != null)
            {
                GUI.DrawTexture(
                    iconRect,
                    icon,
                    ScaleMode.ScaleToFit);
            }

            EditorGUI.LabelField(
                labelRect,
                entry.Name);

            if (showPaths)
            {
                Rect pathRect =
                    new Rect(
                        rowRect.x + 22f,
                        rowRect.y + 17f,
                        rowRect.width - 22f,
                        16f);

                EditorGUI.LabelField(
                    pathRect,
                    entry.Path,
                    EditorStyles.miniLabel);
            }

            HandleRowInteraction(
                rowRect,
                entry);
        }

        /// <summary>
        /// Handles click and double-click behaviour for an asset row.
        /// </summary>
        private static void HandleRowInteraction(
            Rect rowRect,
            ScriptableObjectEntry entry)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseDown ||
                !rowRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.button != 0)
                return;

            Selection.activeObject = entry.Asset;
            EditorGUIUtility.PingObject(entry.Asset);

            if (currentEvent.clickCount >= 2)
            {
                AssetDatabase.OpenAsset(entry.Asset);
            }

            currentEvent.Use();
        }

        /// <summary>
        /// Returns entries matching the current search.
        /// Search checks asset name, type name, and project path.
        /// </summary>
        private List<ScriptableObjectEntry> GetFilteredEntries()
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new List<ScriptableObjectEntry>(entries);
            }

            string query =
                searchText.Trim();

            return entries
                .Where(entry =>
                    entry.Name.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase) ||
                    entry.Type.Name.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase) ||
                    entry.Path.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Finds all ScriptableObject assets in the project.
        /// </summary>
        private void RefreshAssets()
        {
            entries.Clear();
            foldoutStates.Clear();

            string[] guids =
                AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (string guid in guids)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(guid);

                ScriptableObject asset =
                    AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset == null)
                    continue;

                Type type =
                    asset.GetType();

                entries.Add(
                    new ScriptableObjectEntry
                    {
                        Name = asset.name,
                        Path = path,
                        Type = type,
                        Asset = asset
                    });
            }

            entries.Sort(
                (left, right) =>
                {
                    int typeComparison =
                        string.Compare(
                            left.Type.Name,
                            right.Type.Name,
                            StringComparison.OrdinalIgnoreCase);

                    if (typeComparison != 0)
                        return typeComparison;

                    return string.Compare(
                        left.Name,
                        right.Name,
                        StringComparison.OrdinalIgnoreCase);
                });

            assetCount = entries.Count;

            Repaint();
        }
    }
}
