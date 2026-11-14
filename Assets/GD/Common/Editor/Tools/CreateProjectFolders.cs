using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Creates a consistent Unity project folder structure.
    /// Access from "Tools/GD/Setup/Create Project Folders".
    /// </summary>
    public class CreateProjectFolders : EditorWindow
    {
        private enum FolderLayout
        {
            ProjectAssetsOnly,
            ProjectAndThirdPartyAssets
        }

        private const string DefaultProjectFolderName = "_Project";
        private const string ThirdPartyFolderName = "ThirdParty";

        private static readonly string[] ProjectFolders =
        {
            "Animations",
            "Audio",
            "Data",
            "Materials",
            "Meshes",
            "Prefabs",
            "Scenes",
            "Scripts",
            "Settings",
            "Shaders",
            "Textures",
            "UI"
        };

        private string projectFolderName = DefaultProjectFolderName;
        private FolderLayout folderLayout = FolderLayout.ProjectAssetsOnly;
        private bool showPreview = true;
        private string statusMessage;
        private MessageType statusMessageType = MessageType.None;

        /// <summary>
        /// Opens the project structure creation window.
        /// </summary>
        [MenuItem("Tools/GD/Setup/Create Project Folders", priority = 0)]
        private static void ShowWindow()
        {
            CreateProjectFolders window = GetWindow<CreateProjectFolders>();
            window.titleContent = new GUIContent("Create Project Folders");
            window.minSize = new Vector2(420f, 450f);
            window.Show();
        }

        /// <summary>
        /// Draws the editor window.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(10f);

            DrawProjectFolderField();

            GUILayout.Space(10f);

            DrawFolderLayoutOptions();

            GUILayout.Space(10f);

            DrawPreview();

            GUILayout.FlexibleSpace();

            DrawStatusMessage();
            DrawButtons();
        }

        /// <summary>
        /// Draws the heading and introductory guidance.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.LabelField("Create Project Structure", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Create a standard folder structure for your Unity game project. " +
                "Existing folders are left unchanged and any missing folders are added.",
                MessageType.Info);
        }

        /// <summary>
        /// Draws the project folder name field and any validation feedback.
        /// </summary>
        private void DrawProjectFolderField()
        {
            EditorGUILayout.LabelField("Project Folder", EditorStyles.boldLabel);

            projectFolderName = EditorGUILayout.TextField(projectFolderName).Trim();

            string validationMessage = GetValidationMessage();

            if (!string.IsNullOrEmpty(validationMessage))
            {
                EditorGUILayout.HelpBox(validationMessage, MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField(
                    $"Assets/{projectFolderName}",
                    EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// Draws the mutually exclusive folder layout options.
        /// </summary>
        private void DrawFolderLayoutOptions()
        {
            EditorGUILayout.LabelField("Folder Structure", EditorStyles.boldLabel);

            bool projectOnlySelected = folderLayout == FolderLayout.ProjectAssetsOnly;
            bool projectAndThirdPartySelected =
                folderLayout == FolderLayout.ProjectAndThirdPartyAssets;

            bool newProjectOnlySelected = EditorGUILayout.ToggleLeft(
                "Project assets only",
                projectOnlySelected);

            if (newProjectOnlySelected && !projectOnlySelected)
            {
                folderLayout = FolderLayout.ProjectAssetsOnly;
                ClearStatus();
            }

            EditorGUILayout.LabelField(
                "Creates the standard structure for content developed specifically for this project.",
                EditorStyles.wordWrappedMiniLabel);

            GUILayout.Space(4f);

            bool newProjectAndThirdPartySelected = EditorGUILayout.ToggleLeft(
                "Project + third-party assets",
                projectAndThirdPartySelected);

            if (newProjectAndThirdPartySelected && !projectAndThirdPartySelected)
            {
                folderLayout = FolderLayout.ProjectAndThirdPartyAssets;
                ClearStatus();
            }

            EditorGUILayout.LabelField(
                $"Also creates Assets/{ThirdPartyFolderName} for imported packages and external assets.",
                EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>
        /// Draws a live preview of the folders that will be created.
        /// </summary>
        private void DrawPreview()
        {
            showPreview = EditorGUILayout.Foldout(showPreview, "Preview folders", true);

            if (!showPreview)
                return;

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"Assets/{projectFolderName}");

            EditorGUI.indentLevel++;

            foreach (string folder in ProjectFolders)
            {
                EditorGUILayout.LabelField(folder);
            }

            EditorGUI.indentLevel--;

            if (folderLayout == FolderLayout.ProjectAndThirdPartyAssets)
            {
                EditorGUILayout.LabelField($"Assets/{ThirdPartyFolderName}");
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Draws the status message shown after folder creation.
        /// </summary>
        private void DrawStatusMessage()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusMessageType);
                GUILayout.Space(4f);
            }
        }

        /// <summary>
        /// Draws the Cancel and Create buttons.
        /// </summary>
        private void DrawButtons()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Cancel", GUILayout.Height(28f)))
            {
                Close();
            }

            bool isValid = string.IsNullOrEmpty(GetValidationMessage());

            using (new EditorGUI.DisabledScope(!isValid))
            {
                if (GUILayout.Button("Create Folders", GUILayout.Height(28f)))
                {
                    CreateProjectStructure();
                }
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates the selected project folder structure.
        /// Existing folders are preserved.
        /// </summary>
        private void CreateProjectStructure()
        {
            ClearStatus();

            string projectRoot = $"Assets/{projectFolderName}";

            try
            {
                CreateFolderPath(projectRoot);

                foreach (string folder in ProjectFolders)
                {
                    CreateFolderPath($"{projectRoot}/{folder}");
                }

                if (folderLayout == FolderLayout.ProjectAndThirdPartyAssets)
                {
                    CreateFolderPath($"Assets/{ThirdPartyFolderName}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                PingFolder(projectRoot);

                statusMessage =
                    $"Project structure is ready at {projectRoot}. Existing folders were left unchanged.";
                statusMessageType = MessageType.Info;
            }
            catch (Exception exception)
            {
                statusMessage = $"Could not create the project structure:\n{exception.Message}";
                statusMessageType = MessageType.Error;
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Creates a folder path recursively using Unity's AssetDatabase.
        /// Existing folders are ignored.
        /// </summary>
        /// <param name="path">Unity-relative folder path, starting with Assets.</param>
        private static void CreateFolderPath(string path)
        {
            string normalizedPath = path.Replace('\\', '/').TrimEnd('/');

            if (AssetDatabase.IsValidFolder(normalizedPath))
                return;

            string[] parts = normalizedPath.Split('/');

            if (parts.Length == 0 || parts[0] != "Assets")
            {
                throw new ArgumentException(
                    "Folder paths must be located inside the Assets folder.",
                    nameof(path));
            }

            string currentPath = "Assets";

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    string guid = AssetDatabase.CreateFolder(currentPath, parts[i]);

                    if (string.IsNullOrEmpty(guid))
                    {
                        throw new IOException($"Unity could not create folder: {nextPath}");
                    }
                }

                currentPath = nextPath;
            }
        }

        /// <summary>
        /// Selects and highlights the created project root in the Project window.
        /// </summary>
        /// <param name="path">Unity-relative folder path.</param>
        private static void PingFolder(string path)
        {
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            if (folder == null)
                return;

            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        /// <summary>
        /// Returns a validation message for the project folder name,
        /// or an empty string when the name is valid.
        /// </summary>
        private string GetValidationMessage()
        {
            if (string.IsNullOrWhiteSpace(projectFolderName))
            {
                return "Enter a project folder name.";
            }

            if (projectFolderName == "." || projectFolderName == "..")
            {
                return "Enter a valid folder name.";
            }

            if (projectFolderName.Contains("/") || projectFolderName.Contains("\\"))
            {
                return "Folder names cannot contain / or \\ characters.";
            }

            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            if (projectFolderName.IndexOfAny(invalidCharacters) >= 0)
            {
                return "The project folder name contains invalid characters.";
            }

            if (string.Equals(projectFolderName, ThirdPartyFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return $"The project folder cannot be named '{ThirdPartyFolderName}'.";
            }

            return string.Empty;
        }

        /// <summary>
        /// Clears any previous status message.
        /// </summary>
        private void ClearStatus()
        {
            statusMessage = string.Empty;
            statusMessageType = MessageType.None;
        }
    }
}
