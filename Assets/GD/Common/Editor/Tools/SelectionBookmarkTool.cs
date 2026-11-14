using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Provides a simple bookmark list for frequently used scene objects and project assets.
    /// Bookmarks are stored locally in EditorPrefs for the current Unity project.
    /// </summary>
    public class SelectionBookmarkTool : EditorWindow
    {
        [Serializable]
        private class Bookmark
        {
            public string Name;
            public string GlobalObjectId;
            public string AssetGuid;
            public string AssetPath;
            public bool IsSceneObject;
        }

        [Serializable]
        private class BookmarkCollection
        {
            public List<Bookmark> Items = new List<Bookmark>();
        }

        private readonly List<Bookmark> bookmarks = new List<Bookmark>();

        private Vector2 scrollPosition;
        private string newBookmarkName = string.Empty;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.None;

        /// <summary>
        /// Opens the Selection Bookmark tool.
        /// </summary>
        [MenuItem("Tools/GD/Navigate/Selection Bookmarks", priority = 100)]
        private static void ShowWindow()
        {
            SelectionBookmarkTool window =
                GetWindow<SelectionBookmarkTool>();

            window.titleContent =
                new GUIContent("Selection Bookmarks");

            window.minSize =
                new Vector2(420f, 360f);

            window.Show();
        }

        /// <summary>
        /// Loads saved bookmarks.
        /// </summary>
        private void OnEnable()
        {
            LoadBookmarks();
        }

        /// <summary>
        /// Draws the bookmark interface.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(8f);

            DrawAddBookmarkSection();

            GUILayout.Space(8f);

            DrawBookmarks();

            GUILayout.FlexibleSpace();

            DrawStatus();
        }

        /// <summary>
        /// Draws heading and usage guidance.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "Selection Bookmarks",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Bookmark important GameObjects, components, scripts, prefabs, " +
                "ScriptableObjects, and other project assets for quick navigation.",
                MessageType.Info);
        }

        /// <summary>
        /// Draws controls for adding the current selection.
        /// </summary>
        private void DrawAddBookmarkSection()
        {
            EditorGUILayout.LabelField(
                "Add Current Selection",
                EditorStyles.boldLabel);

            UnityEngine.Object selected =
                Selection.activeObject;

            EditorGUILayout.ObjectField(
                "Selection",
                selected,
                typeof(UnityEngine.Object),
                true);

            newBookmarkName =
                EditorGUILayout.TextField(
                    "Bookmark Name",
                    newBookmarkName);

            bool canAdd =
                selected != null;

            using (new EditorGUI.DisabledScope(!canAdd))
            {
                if (GUILayout.Button(
                    "Add Bookmark",
                    GUILayout.Height(28f)))
                {
                    AddCurrentSelection();
                }
            }
        }

        /// <summary>
        /// Draws all bookmarks.
        /// </summary>
        private void DrawBookmarks()
        {
            EditorGUILayout.LabelField(
                $"Bookmarks ({bookmarks.Count})",
                EditorStyles.boldLabel);

            if (bookmarks.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No bookmarks have been added yet.",
                    MessageType.None);

                return;
            }

            scrollPosition =
                EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < bookmarks.Count; i++)
            {
                DrawBookmarkRow(i);
                GUILayout.Space(4f);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws one bookmark entry.
        /// </summary>
        private void DrawBookmarkRow(int index)
        {
            Bookmark bookmark =
                bookmarks[index];

            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox);

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                bookmark.Name,
                EditorStyles.boldLabel);

            if (GUILayout.Button(
                    "Select",
                    GUILayout.Width(55f)))
            {
                SelectBookmark(bookmark);
            }

            if (GUILayout.Button(
                    "Ping",
                    GUILayout.Width(45f)))
            {
                PingBookmark(bookmark);
            }

            if (GUILayout.Button(
                    "X",
                    GUILayout.Width(24f)))
            {
                bookmarks.RemoveAt(index);
                SaveBookmarks();
                ClearStatus();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                GUIUtility.ExitGUI();
                return;
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                bookmark.IsSceneObject
                    ? "Scene Object"
                    : bookmark.AssetPath,
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws status feedback.
        /// </summary>
        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(
                    statusMessage,
                    statusType);
            }
        }

        /// <summary>
        /// Adds the current Unity selection as a bookmark.
        /// </summary>
        private void AddCurrentSelection()
        {
            UnityEngine.Object selected =
                Selection.activeObject;

            if (selected == null)
                return;

            Bookmark bookmark =
                CreateBookmark(selected);

            if (bookmark == null)
            {
                statusMessage =
                    "The selected object could not be bookmarked.";

                statusType =
                    MessageType.Warning;

                return;
            }

            if (ContainsDuplicate(bookmark))
            {
                statusMessage =
                    "That object is already bookmarked.";

                statusType =
                    MessageType.Info;

                return;
            }

            bookmarks.Add(bookmark);
            SaveBookmarks();

            newBookmarkName =
                string.Empty;

            statusMessage =
                $"Bookmarked {bookmark.Name}.";

            statusType =
                MessageType.Info;
        }

        /// <summary>
        /// Creates a bookmark from a Unity object.
        /// </summary>
        private Bookmark CreateBookmark(
            UnityEngine.Object selected)
        {
            string customName =
                string.IsNullOrWhiteSpace(newBookmarkName)
                    ? selected.name
                    : newBookmarkName.Trim();

            GlobalObjectId globalId =
    GlobalObjectId.GetGlobalObjectIdSlow(selected);

            string globalIdString =
                globalId.ToString();

            if (!string.IsNullOrEmpty(globalIdString) &&
                globalId.identifierType != 0)
            {
                return new Bookmark
                {
                    Name = customName,
                    GlobalObjectId = globalIdString,
                    IsSceneObject =
                        !EditorUtility.IsPersistent(selected)
                };
            }

            string path =
                AssetDatabase.GetAssetPath(selected);

            if (string.IsNullOrEmpty(path))
                return null;

            string guid =
                AssetDatabase.AssetPathToGUID(path);

            return new Bookmark
            {
                Name = customName,
                AssetGuid = guid,
                AssetPath = path,
                IsSceneObject = false
            };
        }

        /// <summary>
        /// Selects a bookmarked object.
        /// </summary>
        private void SelectBookmark(
            Bookmark bookmark)
        {
            UnityEngine.Object target =
                ResolveBookmark(bookmark);

            if (target == null)
            {
                statusMessage =
                    $"Could not locate bookmark '{bookmark.Name}'.";

                statusType =
                    MessageType.Warning;

                return;
            }

            Selection.activeObject =
                target;

            statusMessage =
                $"Selected {bookmark.Name}.";

            statusType =
                MessageType.Info;
        }

        /// <summary>
        /// Selects and visually highlights a bookmarked object.
        /// </summary>
        private void PingBookmark(
            Bookmark bookmark)
        {
            UnityEngine.Object target =
                ResolveBookmark(bookmark);

            if (target == null)
            {
                statusMessage =
                    $"Could not locate bookmark '{bookmark.Name}'.";

                statusType =
                    MessageType.Warning;

                return;
            }

            Selection.activeObject =
                target;

            EditorGUIUtility.PingObject(
                target);

            statusMessage =
                $"Pinged {bookmark.Name}.";

            statusType =
                MessageType.Info;
        }

        /// <summary>
        /// Resolves a bookmark back to a Unity object.
        /// </summary>
        private static UnityEngine.Object ResolveBookmark(
            Bookmark bookmark)
        {
            if (!string.IsNullOrEmpty(
                    bookmark.GlobalObjectId))
            {
                GlobalObjectId globalId;

                if (GlobalObjectId.TryParse(
                        bookmark.GlobalObjectId,
                        out globalId))
                {
                    UnityEngine.Object target =
                        GlobalObjectId.GlobalObjectIdentifierToObjectSlow(
                            globalId);

                    if (target != null)
                        return target;
                }
            }

            if (!string.IsNullOrEmpty(
                    bookmark.AssetGuid))
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        bookmark.AssetGuid);

                if (!string.IsNullOrEmpty(path))
                {
                    return AssetDatabase.LoadMainAssetAtPath(
                        path);
                }
            }

            if (!string.IsNullOrEmpty(
                    bookmark.AssetPath))
            {
                return AssetDatabase.LoadMainAssetAtPath(
                    bookmark.AssetPath);
            }

            return null;
        }

        /// <summary>
        /// Returns true when an equivalent bookmark already exists.
        /// </summary>
        private bool ContainsDuplicate(
            Bookmark candidate)
        {
            foreach (Bookmark bookmark in bookmarks)
            {
                if (!string.IsNullOrEmpty(candidate.GlobalObjectId) &&
                    candidate.GlobalObjectId ==
                    bookmark.GlobalObjectId)
                {
                    return true;
                }

                if (!string.IsNullOrEmpty(candidate.AssetGuid) &&
                    candidate.AssetGuid ==
                    bookmark.AssetGuid)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Loads bookmarks from EditorPrefs.
        /// </summary>
        private void LoadBookmarks()
        {
            bookmarks.Clear();

            string json =
                EditorPrefs.GetString(
                    GetPreferencesKey(),
                    string.Empty);

            if (string.IsNullOrEmpty(json))
                return;

            BookmarkCollection collection =
                JsonUtility.FromJson<BookmarkCollection>(
                    json);

            if (collection?.Items != null)
            {
                bookmarks.AddRange(
                    collection.Items);
            }
        }

        /// <summary>
        /// Saves bookmarks to EditorPrefs.
        /// </summary>
        private void SaveBookmarks()
        {
            BookmarkCollection collection =
                new BookmarkCollection();

            collection.Items.AddRange(
                bookmarks);

            string json =
                JsonUtility.ToJson(
                    collection);

            EditorPrefs.SetString(
                GetPreferencesKey(),
                json);
        }

        /// <summary>
        /// Returns a project-specific EditorPrefs key.
        /// </summary>
        private static string GetPreferencesKey()
        {
            string projectPath =
                Application.dataPath;

            return
                $"GD.SelectionBookmarks.{projectPath.GetHashCode()}";
        }

        /// <summary>
        /// Clears status feedback.
        /// </summary>
        private void ClearStatus()
        {
            statusMessage =
                string.Empty;

            statusType =
                MessageType.None;
        }
    }
}
