using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GD
{
    /// <summary>
    /// Finds scene and prefab usages of a selected MonoBehaviour script.
    /// Select a C# script in the Project window, then run the tool.
    /// </summary>
    public class ScriptUsageFinder : EditorWindow
    {
        private sealed class UsageEntry
        {
            public string AssetPath;
            public string HierarchyPath;
            public string AssetType;
            public UnityEngine.Object Asset;
        }

        private readonly List<UsageEntry> usages = new();

        private MonoScript selectedScript;
        private Type targetType;
        private Vector2 scrollPosition;
        private bool searchScenes = true;
        private bool searchPrefabs = true;
        private string statusMessage = string.Empty;
        private MessageType statusMessageType = MessageType.None;

        /// <summary>
        /// Opens the Script Usage Finder.
        /// </summary>
        [MenuItem("Tools/GD/Analyse/Script Usage Finder", priority = 201)]
        private static void ShowWindow()
        {
            ScriptUsageFinder window =
                GetWindow<ScriptUsageFinder>();

            window.titleContent =
                new GUIContent("Script Usage Finder");

            window.minSize =
                new Vector2(540f, 420f);

            window.Show();
        }

        /// <summary>
        /// Attempts to use the currently selected C# script.
        /// </summary>
        private void OnEnable()
        {
            TryUseCurrentSelection();
        }

        /// <summary>
        /// Draws the editor window.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(6f);

            DrawScriptSelection();

            GUILayout.Space(6f);

            DrawOptions();

            GUILayout.Space(8f);

            DrawSearchButton();

            GUILayout.Space(8f);

            DrawStatus();
            DrawResults();
        }

        /// <summary>
        /// Draws the heading and guidance.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "Script Usage Finder",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Find where a MonoBehaviour script is attached in project scenes and prefabs. " +
                "Select a C# script in the Project window or assign one below.",
                MessageType.Info);
        }

        /// <summary>
        /// Draws the target script field.
        /// </summary>
        private void DrawScriptSelection()
        {
            EditorGUILayout.LabelField(
                "Target Script",
                EditorStyles.boldLabel);

            MonoScript newScript =
                (MonoScript)EditorGUILayout.ObjectField(
                    selectedScript,
                    typeof(MonoScript),
                    false);

            if (newScript != selectedScript)
            {
                selectedScript = newScript;
                ResolveTargetType();
                ClearResults();
            }

            if (selectedScript != null &&
                targetType == null)
            {
                EditorGUILayout.HelpBox(
                    "The selected script does not define a MonoBehaviour class.",
                    MessageType.Warning);
            }
        }

        /// <summary>
        /// Draws search-scope options.
        /// </summary>
        private void DrawOptions()
        {
            EditorGUILayout.LabelField(
                "Search",
                EditorStyles.boldLabel);

            searchScenes =
                EditorGUILayout.ToggleLeft(
                    "Scenes",
                    searchScenes);

            searchPrefabs =
                EditorGUILayout.ToggleLeft(
                    "Prefabs",
                    searchPrefabs);
        }

        /// <summary>
        /// Draws the search button.
        /// </summary>
        private void DrawSearchButton()
        {
            bool canSearch =
                selectedScript != null &&
                targetType != null &&
                (searchScenes || searchPrefabs);

            using (new EditorGUI.DisabledScope(!canSearch))
            {
                if (GUILayout.Button(
                    "Find Usages",
                    GUILayout.Height(30f)))
                {
                    FindUsages();
                }
            }
        }

        /// <summary>
        /// Draws search status.
        /// </summary>
        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(
                    statusMessage,
                    statusMessageType);
            }
        }

        /// <summary>
        /// Draws search results.
        /// </summary>
        private void DrawResults()
        {
            if (usages.Count == 0)
                return;

            EditorGUILayout.LabelField(
                $"Results ({usages.Count})",
                EditorStyles.boldLabel);

            scrollPosition =
                EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (UsageEntry usage in usages)
            {
                DrawUsage(usage);
                GUILayout.Space(4f);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws one usage result.
        /// </summary>
        private static void DrawUsage(
            UsageEntry usage)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox);

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                usage.AssetType,
                GUILayout.Width(55f));

            EditorGUILayout.LabelField(
                usage.HierarchyPath,
                EditorStyles.boldLabel);

            if (GUILayout.Button(
                    "Ping",
                    GUILayout.Width(42f)))
            {
                if (usage.Asset != null)
                {
                    Selection.activeObject =
                        usage.Asset;

                    EditorGUIUtility.PingObject(
                        usage.Asset);
                }
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                usage.AssetPath,
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Searches scenes and prefabs for the selected MonoBehaviour type.
        /// </summary>
        private void FindUsages()
        {
            usages.Clear();
            statusMessage = string.Empty;

            if (targetType == null)
                return;

            try
            {
                if (searchPrefabs)
                {
                    SearchPrefabs();
                }

                if (searchScenes)
                {
                    SearchScenes();
                }

                usages.Sort(
                    (left, right) =>
                    {
                        int assetComparison =
                            string.Compare(
                                left.AssetPath,
                                right.AssetPath,
                                StringComparison.OrdinalIgnoreCase);

                        if (assetComparison != 0)
                            return assetComparison;

                        return string.Compare(
                            left.HierarchyPath,
                            right.HierarchyPath,
                            StringComparison.OrdinalIgnoreCase);
                    });

                statusMessage =
                    usages.Count == 0
                        ? "No usages were found."
                        : $"Found {usages.Count} usage{(usages.Count == 1 ? string.Empty : "s")}.";

                statusMessageType =
                    usages.Count == 0
                        ? MessageType.Info
                        : MessageType.Info;
            }
            catch (Exception exception)
            {
                statusMessage =
                    $"Search failed:\n{exception.Message}";

                statusMessageType =
                    MessageType.Error;

                Debug.LogException(exception);
            }

            Repaint();
        }

        /// <summary>
        /// Searches all project prefabs.
        /// </summary>
        private void SearchPrefabs()
        {
            string[] prefabGuids =
                AssetDatabase.FindAssets(
                    "t:Prefab",
                    new[] { "Assets" });

            foreach (string guid in prefabGuids)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(guid);

                GameObject root =
                    PrefabUtility.LoadPrefabContents(path);

                try
                {
                    Component[] components =
                        root.GetComponentsInChildren(
                            targetType,
                            true);

                    foreach (Component component in components)
                    {
                        usages.Add(
                            new UsageEntry
                            {
                                AssetPath = path,
                                HierarchyPath =
                                    GetHierarchyPath(
                                        component.transform),
                                AssetType = "Prefab",
                                Asset =
                                    AssetDatabase.LoadAssetAtPath<GameObject>(
                                        path)
                            });
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        /// <summary>
        /// Searches all project scenes without permanently modifying
        /// the user's current scene setup.
        /// </summary>
        private void SearchScenes()
        {
            SceneSetup[] originalSetup =
                EditorSceneManager.GetSceneManagerSetup();

            string[] sceneGuids =
                AssetDatabase.FindAssets(
                    "t:Scene",
                    new[] { "Assets" });

            try
            {
                foreach (string guid in sceneGuids)
                {
                    string path =
                        AssetDatabase.GUIDToAssetPath(guid);

                    Scene scene =
                        EditorSceneManager.OpenScene(
                            path,
                            OpenSceneMode.Single);

                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        Component[] components =
                            root.GetComponentsInChildren(
                                targetType,
                                true);

                        foreach (Component component in components)
                        {
                            usages.Add(
                                new UsageEntry
                                {
                                    AssetPath = path,
                                    HierarchyPath =
                                        GetHierarchyPath(
                                            component.transform),
                                    AssetType = "Scene",
                                    Asset =
                                        AssetDatabase.LoadAssetAtPath<SceneAsset>(
                                            path)
                                });
                        }
                    }
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(
                    originalSetup);
            }
        }

        /// <summary>
        /// Resolves the MonoBehaviour type represented by the selected script.
        /// </summary>
        private void ResolveTargetType()
        {
            targetType =
                selectedScript != null
                    ? selectedScript.GetClass()
                    : null;

            if (targetType != null &&
                !typeof(MonoBehaviour).IsAssignableFrom(targetType))
            {
                targetType = null;
            }
        }

        /// <summary>
        /// Uses the currently selected Project-window C# script, if any.
        /// </summary>
        private void TryUseCurrentSelection()
        {
            if (Selection.activeObject is MonoScript monoScript)
            {
                selectedScript = monoScript;
                ResolveTargetType();
            }
        }

        /// <summary>
        /// Builds a readable hierarchy path for a Transform.
        /// </summary>
        private static string GetHierarchyPath(
            Transform transform)
        {
            if (transform == null)
                return string.Empty;

            string path =
                transform.name;

            Transform parent =
                transform.parent;

            while (parent != null)
            {
                path =
                    $"{parent.name}/{path}";

                parent =
                    parent.parent;
            }

            return path;
        }

        /// <summary>
        /// Clears previous search results.
        /// </summary>
        private void ClearResults()
        {
            usages.Clear();
            statusMessage = string.Empty;
            statusMessageType = MessageType.None;
        }
    }
}
