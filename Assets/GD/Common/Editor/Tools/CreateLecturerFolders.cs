using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Creates a simple lecturer folder structure under Assets/GD.
    /// </summary>
    public class CreateLecturerFolders : EditorWindow
    {
        private const string BasePath = "Assets/GD";

        private static readonly string[] LecturerFolders =
        {
            "Scripts",
            "Scenes/Start",
            "Scenes/End",
            "Data"
        };

        private string folderName = string.Empty;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.None;

        /// <summary>
        /// Opens the lecturer folder creation window.
        /// </summary>
        [MenuItem("Tools/GD/Lecturer/Create Lecturer Folders", priority = 1000)]
        private static void ShowWindow()
        {
            CreateLecturerFolders window =
                GetWindow<CreateLecturerFolders>();

            window.titleContent =
                new GUIContent("Create Lecturer Folders");

            window.minSize =
                new Vector2(360f, 180f);

            window.Show();
        }

        /// <summary>
        /// Draws the editor window.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Create Lecturer Folders",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Creates a named folder under Assets/GD with Scripts, Scenes/Start, Scenes/End, and Data subfolders.",
                MessageType.Info);

            GUILayout.Space(8f);

            folderName =
                EditorGUILayout.TextField(
                    "Folder Name",
                    folderName);

            string validationMessage =
                GetValidationMessage();

            if (!string.IsNullOrEmpty(validationMessage))
            {
                EditorGUILayout.HelpBox(
                    validationMessage,
                    MessageType.Warning);
            }

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(
                    statusMessage,
                    statusType);
            }

            using (new EditorGUI.DisabledScope(
                !string.IsNullOrEmpty(validationMessage)))
            {
                if (GUILayout.Button(
                    "Create Folders",
                    GUILayout.Height(28f)))
                {
                    CreateFolders();
                }
            }
        }

        /// <summary>
        /// Creates the requested folder structure.
        /// Existing folders are preserved.
        /// </summary>
        private void CreateFolders()
        {
            statusMessage = string.Empty;

            string trimmedName =
                folderName.Trim();

            string rootPath =
                $"{BasePath}/{trimmedName}";

            try
            {
                CreateFolderPath(BasePath);
                CreateFolderPath(rootPath);

                foreach (string folder in LecturerFolders)
                {
                    CreateFolderPath(
                        $"{rootPath}/{folder}");
                }

                AssetDatabase.Refresh();

                UnityEngine.Object rootFolder =
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                        rootPath);

                if (rootFolder != null)
                {
                    Selection.activeObject =
                        rootFolder;

                    EditorGUIUtility.PingObject(
                        rootFolder);
                }

                statusMessage =
                    $"Created lecturer folders in {rootPath}.";

                statusType =
                    MessageType.Info;
            }
            catch (Exception exception)
            {
                statusMessage =
                    $"Could not create folders:\n{exception.Message}";

                statusType =
                    MessageType.Error;

                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// Creates a Unity folder path recursively.
        /// Existing folders are ignored.
        /// </summary>
        private static void CreateFolderPath(
            string path)
        {
            string normalizedPath =
                path.Replace('\\', '/')
                    .TrimEnd('/');

            if (AssetDatabase.IsValidFolder(normalizedPath))
                return;

            string[] parts =
                normalizedPath.Split('/');

            if (parts.Length == 0 ||
                parts[0] != "Assets")
            {
                throw new ArgumentException(
                    "Folder paths must be inside Assets.",
                    nameof(path));
            }

            string currentPath =
                "Assets";

            for (int i = 1;
                 i < parts.Length;
                 i++)
            {
                string nextPath =
                    $"{currentPath}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    string guid =
                        AssetDatabase.CreateFolder(
                            currentPath,
                            parts[i]);

                    if (string.IsNullOrEmpty(guid))
                    {
                        throw new IOException(
                            $"Unity could not create folder: {nextPath}");
                    }
                }

                currentPath =
                    nextPath;
            }
        }

        /// <summary>
        /// Validates the folder name.
        /// </summary>
        private string GetValidationMessage()
        {
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return "Enter a folder name.";
            }

            string trimmedName =
                folderName.Trim();

            if (trimmedName.Contains("/") ||
                trimmedName.Contains("\\"))
            {
                return "Folder names cannot contain / or \\ characters.";
            }

            if (trimmedName.IndexOfAny(
                    Path.GetInvalidFileNameChars()) >= 0)
            {
                return "The folder name contains invalid characters.";
            }

            return string.Empty;
        }
    }
}
