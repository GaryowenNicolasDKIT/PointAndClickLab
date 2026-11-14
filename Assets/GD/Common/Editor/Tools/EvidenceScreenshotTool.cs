using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GD
{
    /// <summary>
    /// Captures a camera screenshot for technical evidence and optionally adds
    /// a visible metadata footer. A matching Markdown evidence file is also created.
    /// </summary>
    public class EvidenceScreenshotTool : EditorWindow
    {
        private const string DefaultFolder = "Evidence/Screenshots";

        private Camera targetCamera;
        private new string title = "Technical Evidence";
        private string notes = string.Empty;

        private int width = 1920;
        private int height = 1080;

        private bool includeFooter = true;
        private bool includeTimestamp = true;
        private bool includeUnityVersion = true;
        private bool includeSceneName = true;
        private bool includeProjectName = true;

        private string outputFolder = DefaultFolder;

        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.None;

        /// <summary>
        /// Opens the Evidence Screenshot tool.
        /// </summary>
        [MenuItem("Tools/GD/Evidence/Evidence Screenshot", priority = 300)]
        private static void ShowWindow()
        {
            EvidenceScreenshotTool window =
                GetWindow<EvidenceScreenshotTool>();

            window.titleContent =
                new GUIContent("Evidence Screenshot");

            window.minSize =
                new Vector2(440f, 500f);

            window.Show();
        }

        /// <summary>
        /// Attempts to use the current Main Camera when the window opens.
        /// </summary>
        private void OnEnable()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        /// <summary>
        /// Draws the tool UI.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(8f);

            DrawCaptureSettings();

            GUILayout.Space(8f);

            DrawMetadataSettings();

            GUILayout.Space(8f);

            DrawOutputSettings();

            GUILayout.FlexibleSpace();

            DrawStatus();
            DrawCaptureButton();
        }

        /// <summary>
        /// Draws heading and guidance.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "Evidence Screenshot",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Capture a high-resolution screenshot from a selected Camera. " +
                "The tool can add a visible evidence footer and creates a matching Markdown record.",
                MessageType.Info);
        }

        /// <summary>
        /// Draws camera, title, notes and resolution controls.
        /// </summary>
        private void DrawCaptureSettings()
        {
            EditorGUILayout.LabelField(
                "Capture",
                EditorStyles.boldLabel);

            targetCamera =
                (Camera)EditorGUILayout.ObjectField(
                    "Camera",
                    targetCamera,
                    typeof(Camera),
                    true);

            title =
                EditorGUILayout.TextField(
                    "Evidence Title",
                    title);

            EditorGUILayout.LabelField(
                "Notes");

            notes =
                EditorGUILayout.TextArea(
                    notes,
                    GUILayout.Height(70f));

            width =
                Mathf.Max(
                    1,
                    EditorGUILayout.IntField(
                        "Width",
                        width));

            height =
                Mathf.Max(
                    1,
                    EditorGUILayout.IntField(
                        "Height",
                        height));

            GUILayout.BeginHorizontal();

            GUILayout.Space(EditorGUIUtility.labelWidth);

            if (GUILayout.Button("1920 × 1080"))
            {
                width = 1920;
                height = 1080;
            }

            if (GUILayout.Button("2560 × 1440"))
            {
                width = 2560;
                height = 1440;
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws visible metadata options.
        /// </summary>
        private void DrawMetadataSettings()
        {
            EditorGUILayout.LabelField(
                "Evidence Metadata",
                EditorStyles.boldLabel);

            includeFooter =
                EditorGUILayout.Toggle(
                    "Visible Footer",
                    includeFooter);

            using (new EditorGUI.DisabledScope(!includeFooter))
            {
                includeProjectName =
                    EditorGUILayout.Toggle(
                        "Project Name",
                        includeProjectName);

                includeSceneName =
                    EditorGUILayout.Toggle(
                        "Scene Name",
                        includeSceneName);

                includeUnityVersion =
                    EditorGUILayout.Toggle(
                        "Unity Version",
                        includeUnityVersion);

                includeTimestamp =
                    EditorGUILayout.Toggle(
                        "Timestamp",
                        includeTimestamp);
            }
        }

        /// <summary>
        /// Draws output folder controls.
        /// </summary>
        private void DrawOutputSettings()
        {
            EditorGUILayout.LabelField(
                "Output",
                EditorStyles.boldLabel);

            outputFolder =
                EditorGUILayout.TextField(
                    "Project Folder",
                    outputFolder);

            EditorGUILayout.LabelField(
                $"Saved under: {GetProjectRootPath()}",
                EditorStyles.miniLabel);
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
                    statusType);

                GUILayout.Space(4f);
            }
        }

        /// <summary>
        /// Draws the screenshot capture button.
        /// </summary>
        private void DrawCaptureButton()
        {
            bool valid =
                targetCamera != null &&
                width > 0 &&
                height > 0 &&
                !string.IsNullOrWhiteSpace(outputFolder);

            using (new EditorGUI.DisabledScope(!valid))
            {
                if (GUILayout.Button(
                    "Capture Evidence Screenshot",
                    GUILayout.Height(32f)))
                {
                    CaptureEvidence();
                }
            }
        }

        /// <summary>
        /// Captures the selected camera and writes PNG and Markdown evidence files.
        /// </summary>
        private void CaptureEvidence()
        {
            ClearStatus();

            string projectRoot =
                GetProjectRootPath();

            string absoluteOutputFolder =
                Path.Combine(
                    projectRoot,
                    outputFolder);

            Directory.CreateDirectory(
                absoluteOutputFolder);

            DateTime now =
                DateTime.Now;

            string baseFileName =
                BuildBaseFileName(now);

            string pngPath =
                Path.Combine(
                    absoluteOutputFolder,
                    $"{baseFileName}.png");

            string markdownPath =
                Path.Combine(
                    absoluteOutputFolder,
                    $"{baseFileName}.md");

            RenderTexture renderTexture = null;
            Texture2D screenshot = null;
            GameObject footerCanvasObject = null;

            RenderTexture previousTarget =
                targetCamera.targetTexture;

            RenderTexture previousActive =
                RenderTexture.active;

            try
            {
                if (includeFooter)
                {
                    footerCanvasObject =
                        CreateEvidenceFooter(
                            targetCamera,
                            now);
                }

                Canvas.ForceUpdateCanvases();

                renderTexture =
                    new RenderTexture(
                        width,
                        height,
                        24,
                        RenderTextureFormat.ARGB32);

                renderTexture.antiAliasing = 1;

                targetCamera.targetTexture =
                    renderTexture;

                RenderTexture.active =
                    renderTexture;

                targetCamera.Render();

                screenshot =
                    new Texture2D(
                        width,
                        height,
                        TextureFormat.RGB24,
                        false);

                screenshot.ReadPixels(
                    new Rect(
                        0,
                        0,
                        width,
                        height),
                    0,
                    0);

                screenshot.Apply();

                byte[] pngBytes =
                    screenshot.EncodeToPNG();

                File.WriteAllBytes(
                    pngPath,
                    pngBytes);

                File.WriteAllText(
                    markdownPath,
                    BuildMarkdownEvidence(
                        now,
                        pngPath));

                AssetDatabase.Refresh();

                string relativePngPath =
                    GetProjectRelativePath(
                        pngPath);

                UnityEngine.Object pngAsset =
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                        relativePngPath);

                if (pngAsset != null)
                {
                    Selection.activeObject =
                        pngAsset;

                    EditorGUIUtility.PingObject(
                        pngAsset);
                }

                statusMessage =
                    $"Evidence captured: {relativePngPath}";

                statusType =
                    MessageType.Info;
            }
            catch (Exception exception)
            {
                statusMessage =
                    $"Capture failed:\n{exception.Message}";

                statusType =
                    MessageType.Error;

                Debug.LogException(exception);
            }
            finally
            {
                targetCamera.targetTexture =
                    previousTarget;

                RenderTexture.active =
                    previousActive;

                if (footerCanvasObject != null)
                {
                    DestroyImmediate(
                        footerCanvasObject);
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    DestroyImmediate(
                        renderTexture);
                }

                if (screenshot != null)
                {
                    DestroyImmediate(
                        screenshot);
                }
            }
        }

        /// <summary>
        /// Creates a temporary Screen Space Camera footer so metadata appears
        /// directly in the captured image.
        /// </summary>
        private GameObject CreateEvidenceFooter(
            Camera camera,
            DateTime timestamp)
        {
            GameObject canvasObject =
                new GameObject(
                    "__EvidenceFooterCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler));

            canvasObject.hideFlags =
                HideFlags.HideAndDontSave;

            Canvas canvas =
                canvasObject.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceCamera;

            canvas.worldCamera =
                camera;

            canvas.planeDistance =
                Mathf.Max(
                    camera.nearClipPlane + 0.01f,
                    0.1f);

            canvas.sortingOrder =
                short.MaxValue;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(
                    width,
                    height);

            GameObject panelObject =
                new GameObject(
                    "Footer",
                    typeof(RectTransform),
                    typeof(Image));

            panelObject.transform.SetParent(
                canvasObject.transform,
                false);

            RectTransform panelRect =
                panelObject.GetComponent<RectTransform>();

            panelRect.anchorMin =
                new Vector2(0f, 0f);

            panelRect.anchorMax =
                new Vector2(1f, 0f);

            panelRect.pivot =
                new Vector2(0.5f, 0f);

            panelRect.sizeDelta =
                new Vector2(
                    0f,
                    86f);

            panelRect.anchoredPosition =
                Vector2.zero;

            Image panelImage =
                panelObject.GetComponent<Image>();

            panelImage.color =
                new Color(
                    0f,
                    0f,
                    0f,
                    0.78f);

            GameObject textObject =
                new GameObject(
                    "Text",
                    typeof(RectTransform),
                    typeof(Text));

            textObject.transform.SetParent(
                panelObject.transform,
                false);

            RectTransform textRect =
                textObject.GetComponent<RectTransform>();

            textRect.anchorMin =
                Vector2.zero;

            textRect.anchorMax =
                Vector2.one;

            textRect.offsetMin =
                new Vector2(
                    18f,
                    8f);

            textRect.offsetMax =
                new Vector2(
                    -18f,
                    -8f);

            Text text =
                textObject.GetComponent<Text>();

            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            text.fontSize =
                20;

            text.alignment =
                TextAnchor.MiddleLeft;

            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;

            text.verticalOverflow =
                VerticalWrapMode.Truncate;

            text.color =
                Color.white;

            text.text =
                BuildFooterText(
                    timestamp);

            return canvasObject;
        }

        /// <summary>
        /// Builds the visible footer metadata.
        /// </summary>
        private string BuildFooterText(
            DateTime timestamp)
        {
            string firstLine =
                string.IsNullOrWhiteSpace(title)
                    ? "Technical Evidence"
                    : title.Trim();

            string secondLine =
                BuildMetadataLine(
                    timestamp);

            if (!string.IsNullOrWhiteSpace(notes))
            {
                return $"{firstLine} — {notes.Trim()}\n{secondLine}";
            }

            return $"{firstLine}\n{secondLine}";
        }

        /// <summary>
        /// Builds the compact metadata line for the visible footer.
        /// </summary>
        private string BuildMetadataLine(
            DateTime timestamp)
        {
            var parts =
                new System.Collections.Generic.List<string>();

            if (includeProjectName)
            {
                parts.Add(
                    $"Project: {GetProjectName()}");
            }

            if (includeSceneName)
            {
                string sceneName =
                    SceneManager.GetActiveScene().name;

                parts.Add(
                    $"Scene: {(string.IsNullOrEmpty(sceneName) ? "Untitled" : sceneName)}");
            }

            if (includeUnityVersion)
            {
                parts.Add(
                    $"Unity: {Application.unityVersion}");
            }

            if (includeTimestamp)
            {
                parts.Add(
                    $"Captured: {timestamp:yyyy-MM-dd HH:mm:ss}");
            }

            return string.Join(
                " | ",
                parts);
        }

        /// <summary>
        /// Builds the Markdown evidence record.
        /// </summary>
        private string BuildMarkdownEvidence(
            DateTime timestamp,
            string pngPath)
        {
            string sceneName =
                SceneManager.GetActiveScene().name;

            return
$@"# {SanitiseMarkdown(title)}

- **Project:** {GetProjectName()}
- **Scene:** {(string.IsNullOrEmpty(sceneName) ? "Untitled" : sceneName)}
- **Unity Version:** {Application.unityVersion}
- **Captured:** {timestamp:yyyy-MM-dd HH:mm:ss}
- **Camera:** {targetCamera.name}
- **Resolution:** {width} × {height}
- **Screenshot:** {Path.GetFileName(pngPath)}

## Notes

{(string.IsNullOrWhiteSpace(notes) ? "No notes provided." : notes.Trim())}
";
        }

        /// <summary>
        /// Builds a safe timestamped base filename.
        /// </summary>
        private string BuildBaseFileName(
            DateTime timestamp)
        {
            string safeTitle =
                string.IsNullOrWhiteSpace(title)
                    ? "Evidence"
                    : title.Trim();

            foreach (char invalidCharacter in
                     Path.GetInvalidFileNameChars())
            {
                safeTitle =
                    safeTitle.Replace(
                        invalidCharacter,
                        '_');
            }

            safeTitle =
                safeTitle.Replace(
                    ' ',
                    '_');

            return
                $"{timestamp:yyyyMMdd_HHmmss}_{safeTitle}";
        }

        /// <summary>
        /// Returns the Unity project root directory.
        /// </summary>
        private static string GetProjectRootPath()
        {
            return Directory.GetParent(
                Application.dataPath).FullName;
        }

        /// <summary>
        /// Returns the project folder name.
        /// </summary>
        private static string GetProjectName()
        {
            return new DirectoryInfo(
                GetProjectRootPath()).Name;
        }

        /// <summary>
        /// Converts an absolute path inside the project into a Unity-relative path.
        /// </summary>
        private static string GetProjectRelativePath(
            string absolutePath)
        {
            string projectRoot =
                GetProjectRootPath()
                    .Replace('\\', '/');

            string normalizedPath =
                absolutePath.Replace('\\', '/');

            if (!normalizedPath.StartsWith(
                    projectRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath;
            }

            return normalizedPath
                .Substring(projectRoot.Length + 1);
        }

        /// <summary>
        /// Performs minimal Markdown sanitisation.
        /// </summary>
        private static string SanitiseMarkdown(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Technical Evidence";

            return value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
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

