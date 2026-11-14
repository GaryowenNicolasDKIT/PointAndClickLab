using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Creates a standard set of top-level empty GameObjects
    /// to help organise a Unity scene hierarchy.
    /// </summary>
    public class SceneHierarchyFolders : EditorWindow
    {
        private static readonly string[] DefaultGroups =
        {
            "Cameras",
            "Lighting",
            "Environment",
            "Gameplay",
            "Props",
            "Audio",
            "UI",
            "Systems"
        };

        private readonly Dictionary<string, bool> selectedGroups = new();

        private Vector2 scrollPosition;
        private bool showPreview = true;
        private string statusMessage = string.Empty;
        private MessageType statusMessageType = MessageType.None;

        /// <summary>
        /// Opens the scene hierarchy folder generator.
        /// </summary>
        [MenuItem("Tools/GD/Setup/Create Scene Hierarchy", priority = 1)]
        private static void ShowWindow()
        {
            SceneHierarchyFolders window =
                GetWindow<SceneHierarchyFolders>();

            window.titleContent =
                new GUIContent("Create Scene Hierarchy");

            window.minSize =
                new Vector2(390f, 420f);

            window.Show();
        }

        /// <summary>
        /// Initialises the default folder selections.
        /// </summary>
        private void OnEnable()
        {
            foreach (string group in DefaultGroups)
            {
                if (!selectedGroups.ContainsKey(group))
                {
                    selectedGroups[group] = true;
                }
            }
        }

        /// <summary>
        /// Draws the editor window.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(8f);

            DrawGroupSelection();

            GUILayout.Space(8f);

            DrawPreview();

            GUILayout.FlexibleSpace();

            DrawStatus();
            DrawButtons();
        }

        /// <summary>
        /// Draws the heading and explanatory text.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "Create Scene Hierarchy",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Create common top-level empty GameObjects to organise the current scene. " +
                "Existing matching objects are left unchanged.",
                MessageType.Info);
        }

        /// <summary>
        /// Draws checkboxes for the hierarchy groups.
        /// </summary>
        private void DrawGroupSelection()
        {
            EditorGUILayout.LabelField(
                "Hierarchy Groups",
                EditorStyles.boldLabel);

            scrollPosition =
                EditorGUILayout.BeginScrollView(
                    scrollPosition,
                    GUILayout.Height(190f));

            foreach (string group in DefaultGroups)
            {
                bool newValue =
                    EditorGUILayout.ToggleLeft(
                        group,
                        selectedGroups[group]);

                if (newValue != selectedGroups[group])
                {
                    selectedGroups[group] = newValue;
                    ClearStatus();
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All"))
            {
                SetAllSelections(true);
            }

            if (GUILayout.Button("Clear All"))
            {
                SetAllSelections(false);
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws a preview of the hierarchy groups to be created.
        /// </summary>
        private void DrawPreview()
        {
            showPreview =
                EditorGUILayout.Foldout(
                    showPreview,
                    "Preview hierarchy",
                    true);

            if (!showPreview)
                return;

            EditorGUI.indentLevel++;

            string sceneName =
                UnityEngine.SceneManagement.SceneManager
                    .GetActiveScene()
                    .name;

            EditorGUILayout.LabelField(
                string.IsNullOrEmpty(sceneName)
                    ? "Current Scene"
                    : sceneName);

            EditorGUI.indentLevel++;

            bool anySelected = false;

            foreach (string group in DefaultGroups)
            {
                if (!selectedGroups[group])
                    continue;

                EditorGUILayout.LabelField(group);
                anySelected = true;
            }

            if (!anySelected)
            {
                EditorGUILayout.LabelField(
                    "(No groups selected)",
                    EditorStyles.miniLabel);
            }

            EditorGUI.indentLevel -= 2;
        }

        /// <summary>
        /// Draws the current status message.
        /// </summary>
        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(
                    statusMessage,
                    statusMessageType);

                GUILayout.Space(4f);
            }
        }

        /// <summary>
        /// Draws Cancel and Create controls.
        /// </summary>
        private void DrawButtons()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(
                    "Cancel",
                    GUILayout.Height(28f)))
            {
                Close();
            }

            using (new EditorGUI.DisabledScope(!HasSelectedGroups()))
            {
                if (GUILayout.Button(
                        "Create Hierarchy",
                        GUILayout.Height(28f)))
                {
                    CreateHierarchy();
                }
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates any selected top-level hierarchy groups
        /// that do not already exist.
        /// </summary>
        private void CreateHierarchy()
        {
            ClearStatus();

            int createdCount = 0;
            int existingCount = 0;

            foreach (string groupName in DefaultGroups)
            {
                if (!selectedGroups[groupName])
                    continue;

                GameObject existingObject =
                    FindTopLevelObject(groupName);

                if (existingObject != null)
                {
                    existingCount++;
                    continue;
                }

                GameObject groupObject =
                    new GameObject(groupName);

                Undo.RegisterCreatedObjectUndo(
                    groupObject,
                    $"Create {groupName}");

                groupObject.transform.SetSiblingIndex(
                    GetPreferredSiblingIndex(groupName));

                createdCount++;
            }

            statusMessage =
                BuildStatusMessage(
                    createdCount,
                    existingCount);

            statusMessageType =
                MessageType.Info;
        }

        /// <summary>
        /// Finds a top-level GameObject with the supplied name.
        /// </summary>
        private static GameObject FindTopLevelObject(
            string objectName)
        {
            GameObject[] rootObjects =
                UnityEngine.SceneManagement.SceneManager
                    .GetActiveScene()
                    .GetRootGameObjects();

            foreach (GameObject rootObject in rootObjects)
            {
                if (rootObject.name == objectName)
                {
                    return rootObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the preferred sibling index for a hierarchy group
        /// based on the default group order.
        /// </summary>
        private static int GetPreferredSiblingIndex(
            string groupName)
        {
            for (int i = 0; i < DefaultGroups.Length; i++)
            {
                if (DefaultGroups[i] == groupName)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Builds the completion message shown to the user.
        /// </summary>
        private static string BuildStatusMessage(
            int createdCount,
            int existingCount)
        {
            if (createdCount == 0 && existingCount > 0)
            {
                return "No new hierarchy groups were required. " +
                       "All selected groups already exist.";
            }

            if (existingCount == 0)
            {
                return $"Created {createdCount} hierarchy group" +
                       $"{(createdCount == 1 ? string.Empty : "s")}.";
            }

            return $"Created {createdCount} hierarchy group" +
                   $"{(createdCount == 1 ? string.Empty : "s")}. " +
                   $"{existingCount} already existed and were left unchanged.";
        }

        /// <summary>
        /// Sets all hierarchy group selections.
        /// </summary>
        private void SetAllSelections(
            bool selected)
        {
            foreach (string group in DefaultGroups)
            {
                selectedGroups[group] = selected;
            }

            ClearStatus();
        }

        /// <summary>
        /// Returns true when at least one hierarchy group is selected.
        /// </summary>
        private bool HasSelectedGroups()
        {
            foreach (bool selected in selectedGroups.Values)
            {
                if (selected)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Clears the current status message.
        /// </summary>
        private void ClearStatus()
        {
            statusMessage = string.Empty;
            statusMessageType = MessageType.None;
        }
    }
}
