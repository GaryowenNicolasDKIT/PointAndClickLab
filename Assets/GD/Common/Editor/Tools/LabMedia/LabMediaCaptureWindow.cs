using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Captures the images a note or lab asks for. It lists the IMAGE
    /// placeholders in a Markdown file; you set up the editor, capture one or
    /// two panels (editor windows or a camera), check the preview, and the
    /// window saves the PNG to the placeholder's path and replaces the
    /// placeholder with the house-style image block. GIF placeholders open
    /// GIF Capture with the path and alt text filled in, and the placeholder is
    /// replaced when the GIF is saved.
    /// </summary>
    public class LabMediaCaptureWindow : EditorWindow
    {
        private const string WindowTitle = "Lab Media Capture";
        private const float SettleSeconds = 0.25f;
        private const int PanelGap = 8;
        private const float MaxPreviewHeight = 320f;
        private const float GameViewToolbarPoints = 21f;

        private static readonly string[] DocumentFolders = { "Docs/labs", "Docs/notes" };
        private static readonly Color32 PanelBackground = new(40, 40, 40, 255);
        private static readonly Color SelectedColour = new(0.35f, 0.6f, 1f);

        private enum PanelSource
        {
            EditorWindow,
            Camera,
            FullEditor
        }

        /// <summary>
        /// What one panel of a screenshot shows.
        /// </summary>
        [Serializable]
        private class PanelSettings
        {
            public PanelSource Source = PanelSource.EditorWindow;
            public string WindowKey = string.Empty;
            public float CropTopPoints;
            public Camera Camera;
            public int Width = 1280;
            public int Height = 720;
        }

        [SerializeField] private string documentPath = string.Empty;
        [SerializeField] private string selectedSource = string.Empty;
        [SerializeField] private string altText = string.Empty;
        [SerializeField] private bool twoPanels;
        [SerializeField] private PanelSettings leftPanel = new();
        [SerializeField] private PanelSettings rightPanel = new();
        [SerializeField] private int displayWidth = 720;
        [SerializeField] private bool showInserted;
        [SerializeField] private bool showAdvanced;
        [SerializeField] private float screenScaleOverride;
        [SerializeField] private bool flipVertically;

        private LabMediaDocument document;
        private string[] documentPaths = Array.Empty<string>();
        private string[] documentLabels = Array.Empty<string>();
        private readonly List<EditorWindow> openWindows = new();
        private string[] windowLabels = Array.Empty<string>();
        private float detectedScreenScale = 1f;

        // The capture in progress: the panels still to read and those read so far.
        private readonly List<PanelSettings> pendingPanels = new();
        private readonly List<Texture2D> capturedPanels = new();
        private bool isCapturing;
        private double captureAt;

        // The finished capture, shown for checking before it is saved.
        private Texture2D preview;
        private string previewSource;

        private Vector2 scroll;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.None;

        /// <summary>
        /// Opens the Lab Media Capture window.
        /// </summary>
        [MenuItem("Tools/GD/Evidence/Lab Media Capture", priority = 302)]
        private static void ShowWindow()
        {
            LabMediaCaptureWindow window = GetWindow<LabMediaCaptureWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(420f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            // GIF Capture raises this after saving, including in Play mode
            // after the domain reload, because this subscribes again on enable.
            GifCaptureWindow.Saved += OnGifSaved;
            RefreshDocuments();
            RefreshWindows();
        }

        private void OnDisable()
        {
            GifCaptureWindow.Saved -= OnGifSaved;
            EditorApplication.update -= CaptureTick;
            isCapturing = false;
            ClearCapture();
            ClearPreview();
        }

        private void OnFocus()
        {
            // The file may have been edited in an IDE since the window last looked.
            document?.Reload();
        }

        private void OnGUI()
        {
            // pixelsPerPoint is only reliable while a GUI is being drawn.
            detectedScreenScale = EditorGUIUtility.pixelsPerPoint;

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawDocumentPicker();

            if (document != null)
            {
                GUILayout.Space(6f);
                DrawItemList();
                GUILayout.Space(6f);
                DrawSelectedItem();
            }

            if (!string.IsNullOrEmpty(statusMessage))
                EditorGUILayout.HelpBox(statusMessage, statusType);

            EditorGUILayout.EndScrollView();
        }

        #region Documents

        private void RefreshDocuments()
        {
            string root = GetProjectRootPath();

            documentPaths = DocumentFolders
                .Select(folder => Path.Combine(root, folder))
                .Where(Directory.Exists)
                .SelectMany(folder => Directory.GetFiles(folder, "*.md"))
                .Select(GetProjectRelativePath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            documentLabels = documentPaths
                .Select(path =>
                {
                    int count = LabMediaDocument.CountPlaceholders(Path.Combine(root, path));
                    string name = path.Substring("Docs/".Length);
                    return count > 0 ? $"{name} ({count} to capture)" : name;
                })
                .ToArray();

            LoadDocument(documentPath);
        }

        private void LoadDocument(string relativePath)
        {
            documentPath = relativePath ?? string.Empty;
            string fullPath = Path.Combine(GetProjectRootPath(), documentPath);

            document = documentPath.Length > 0 && File.Exists(fullPath) ? new LabMediaDocument(fullPath) : null;
            ClearPreview();
        }

        private void DrawDocumentPicker()
        {
            EditorGUILayout.BeginHorizontal();

            int current = Array.IndexOf(documentPaths, documentPath);
            int chosen = EditorGUILayout.Popup("Document", current, documentLabels);

            if (chosen != current && chosen >= 0)
            {
                LoadDocument(documentPaths[chosen]);
                SelectFirstPlaceholder();
            }

            if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60f)))
            {
                RefreshDocuments();
                RefreshWindows();
            }

            EditorGUILayout.EndHorizontal();

            if (documentPaths.Length == 0)
                EditorGUILayout.HelpBox("No Markdown files found in Docs/labs or Docs/notes.", MessageType.Warning);
        }

        #endregion Documents

        #region Item List

        private void DrawItemList()
        {
            List<LabMediaItem> placeholders = document.Items.Where(item => item.IsPlaceholder).ToList();
            List<LabMediaItem> inserted = document.Items.Where(item => !item.IsPlaceholder).ToList();

            EditorGUILayout.LabelField($"To capture ({placeholders.Count})", EditorStyles.boldLabel);

            if (placeholders.Count == 0)
                EditorGUILayout.LabelField("No screenshot or gif placeholders left in this file.", EditorStyles.miniLabel);

            foreach (LabMediaItem item in placeholders)
                DrawItemRow(item);

            showInserted = EditorGUILayout.Foldout(showInserted, $"Already inserted ({inserted.Count})", true);

            if (!showInserted)
                return;

            foreach (LabMediaItem item in inserted)
                DrawItemRow(item);
        }

        private void DrawItemRow(LabMediaItem item)
        {
            bool isSelected = item.Source == selectedSource;
            string kind = item.Kind == LabMediaKind.Gif ? "GIF" : "PNG";
            string fileState = item.FileExists ? "file saved" : "no file yet";

            Color previous = GUI.contentColor;

            if (isSelected)
                GUI.contentColor = SelectedColour;

            if (GUILayout.Button($"{(isSelected ? ">" : " ")} {kind}  {item.FileName}   ({fileState})", EditorStyles.label))
                Defer(() => Select(item));

            GUI.contentColor = previous;
        }

        private void SelectFirstPlaceholder()
        {
            LabMediaItem first = document?.Items.FirstOrDefault(item => item.IsPlaceholder);

            if (first != null)
                Select(first);
        }

        private void Select(LabMediaItem item)
        {
            selectedSource = item.Source;
            altText = item.Description;

            // An inserted image keeps the width already in the document.
            if (!item.IsPlaceholder && item.Width > 0)
                displayWidth = item.Width;

            ClearPreview();
            GUI.FocusControl(null);
        }

        private LabMediaItem SelectedItem()
        {
            return document?.Items.FirstOrDefault(item => item.Source == selectedSource);
        }

        #endregion Item List

        #region Selected Item

        private void DrawSelectedItem()
        {
            LabMediaItem item = SelectedItem();

            if (item == null)
            {
                EditorGUILayout.HelpBox("Select an image above.", MessageType.None);
                return;
            }

            DrawNavigation(item);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField(item.Source, EditorStyles.boldLabel);

            if (item.IsPlaceholder)
                EditorGUILayout.LabelField(item.Description, EditorStyles.wordWrappedMiniLabel);
            else
                EditorGUILayout.LabelField("Already in the document. A new capture replaces the file and updates the block's alt text and width.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.LabelField("Alt text");
            altText = EditorGUILayout.TextArea(altText, EditorStyles.textArea, GUILayout.MinHeight(48f));

            displayWidth = Mathf.Clamp(EditorGUILayout.IntField("Display width", displayWidth), 120, 1600);

            if (!item.IsPlaceholder)
                DrawInsertedItemButtons(item);

            EditorGUILayout.EndVertical();

            GUILayout.Space(6f);

            if (item.Kind == LabMediaKind.Gif)
                DrawGifControls(item);
            else
                DrawScreenshotControls(item);

            if (item.IsPlaceholder && item.FileExists)
            {
                GUILayout.Space(4f);

                if (GUILayout.Button("Insert Existing File"))
                    InsertExistingFile(item);
            }
        }

        /// <summary>
        /// Steps through every image in reading order, placeholders and
        /// inserted blocks alike.
        /// </summary>
        private void DrawNavigation(LabMediaItem item)
        {
            IReadOnlyList<LabMediaItem> items = document.Items;
            int index = items.ToList().FindIndex(other => other.Source == item.Source);

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(index <= 0 || isCapturing))
            {
                if (GUILayout.Button("< Previous", GUILayout.Width(90f)))
                    Defer(() => Select(items[index - 1]));
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Image {index + 1} of {items.Count}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(index >= items.Count - 1 || isCapturing))
            {
                if (GUILayout.Button("Next >", GUILayout.Width(90f)))
                    Defer(() => Select(items[index + 1]));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawInsertedItemButtons(LabMediaItem item)
        {
            bool changed = altText != item.Description || displayWidth != item.Width;

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!changed))
            {
                if (GUILayout.Button(new GUIContent("Update Block", "Write the alt text and width into the document without capturing again.")))
                    Defer(() => UpdateBlock(item));
            }

            if (GUILayout.Button(new GUIContent("Revert to Placeholder", "Turn the image block back into an IMAGE comment, using the alt text as its description. The image file is kept.")))
                Defer(() => RevertToPlaceholder(item));

            EditorGUILayout.EndHorizontal();
        }

        private void UpdateBlock(LabMediaItem item)
        {
            if (document.UpdateImageBlock(item, altText, displayWidth))
                SetStatus($"Updated the block for {item.FileName}.", MessageType.Info);
            else
                SetStatus("The image block was not found. Press Refresh.", MessageType.Warning);
        }

        private void RevertToPlaceholder(LabMediaItem item)
        {
            if (!EditorUtility.DisplayDialog(
                    WindowTitle,
                    $"Turn the image block for {item.FileName} back into a placeholder?\n\nThe image file is kept, so Insert Existing File can put it back.",
                    "Revert",
                    "Cancel"))
                return;

            if (document.RevertToPlaceholder(item, altText))
            {
                RefreshDocuments();
                SetStatus($"{item.FileName} is a placeholder again.", MessageType.Info);
            }
            else
            {
                SetStatus("The image block was not found. Press Refresh.", MessageType.Warning);
            }
        }

        private void DrawGifControls(LabMediaItem item)
        {
            EditorGUILayout.HelpBox(
                "GIFs record a camera in Play mode. GIF Capture opens with this file name, folder and alt text, " +
                "and replaces an existing file. When you save the take there, this placeholder is replaced.",
                MessageType.None);

            if (GUILayout.Button("Open in GIF Capture", GUILayout.Height(28f)))
            {
                string folder = GetProjectRelativePath(Path.GetDirectoryName(item.FullPath));
                GifCaptureWindow.OpenFor(folder, Path.GetFileNameWithoutExtension(item.FullPath), altText);
            }
        }

        private void DrawScreenshotControls(LabMediaItem item)
        {
            EditorGUILayout.LabelField("Panels", EditorStyles.boldLabel);

            twoPanels = EditorGUILayout.Toggle(new GUIContent("Two panels", "Place two captures side by side, for example the Hierarchy and the Inspector."), twoPanels);

            DrawPanelSettings(twoPanels ? "Left panel" : "Panel", leftPanel);

            if (twoPanels)
                DrawPanelSettings("Right panel", rightPanel);

            DrawAdvancedSettings();

            GUILayout.Space(6f);

            using (new EditorGUI.DisabledScope(isCapturing))
            {
                if (GUILayout.Button(isCapturing ? "Capturing..." : "Capture", GUILayout.Height(30f)))
                    Defer(StartCapture);
            }

            DrawPreview(item);
        }

        private void DrawPanelSettings(string label, PanelSettings panel)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            panel.Source = (PanelSource)EditorGUILayout.EnumPopup("Source", panel.Source);

            if (panel.Source == PanelSource.EditorWindow)
            {
                EditorGUILayout.BeginHorizontal();

                int current = openWindows.FindIndex(window => window != null && KeyFor(window) == panel.WindowKey);
                int chosen = EditorGUILayout.Popup("Window", current, windowLabels);

                if (chosen != current && chosen >= 0)
                {
                    panel.WindowKey = KeyFor(openWindows[chosen]);

                    // The Game view's toolbar is rarely wanted in a lab image.
                    panel.CropTopPoints = openWindows[chosen].GetType().Name == "GameView" ? GameViewToolbarPoints : 0f;
                }

                if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60f)))
                    RefreshWindows();

                EditorGUILayout.EndHorizontal();

                panel.CropTopPoints = Mathf.Max(0f, EditorGUILayout.FloatField(
                    new GUIContent("Crop top (points)", "Removes a toolbar from the top of the window."),
                    panel.CropTopPoints));
            }
            else if (panel.Source == PanelSource.FullEditor)
            {
                EditorGUILayout.LabelField(
                    "Captures the whole main Unity window, with every docked view. " +
                    "If this window is docked in it, it appears too: float it on another screen or drag it aside first.",
                    EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                panel.Camera = (Camera)EditorGUILayout.ObjectField("Camera", panel.Camera, typeof(Camera), true);
                panel.Width = Mathf.Clamp(EditorGUILayout.IntField("Width", panel.Width), 16, 4096);
                panel.Height = Mathf.Clamp(EditorGUILayout.IntField("Height", panel.Height), 16, 4096);
                EditorGUILayout.LabelField("A camera does not draw Screen Space - Overlay UI. Capture the Game view window to include it.", EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedSettings()
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true);

            if (!showAdvanced)
                return;

            EditorGUI.indentLevel++;

            screenScaleOverride = Mathf.Max(0f, EditorGUILayout.FloatField(
                new GUIContent("Screen scale", "Screen pixels per editor point. 0 uses the detected value. Change it if a window capture is cut off or offset."),
                screenScaleOverride));

            EditorGUILayout.LabelField($"Detected: {detectedScreenScale:0.##}", EditorStyles.miniLabel);

            flipVertically = EditorGUILayout.Toggle(
                new GUIContent("Flip vertically", "Use if window captures come out upside down."),
                flipVertically);

            EditorGUI.indentLevel--;
        }

        private void DrawPreview(LabMediaItem item)
        {
            if (preview == null || previewSource != item.Source)
                return;

            GUILayout.Space(6f);
            EditorGUILayout.LabelField($"Preview ({preview.width} x {preview.height} px)", EditorStyles.boldLabel);

            float availableWidth = Mathf.Max(100f, position.width - 30f);
            float aspect = preview.width / (float)preview.height;
            float height = Mathf.Min(MaxPreviewHeight, availableWidth / aspect);

            Rect area = GUILayoutUtility.GetRect(availableWidth, height, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(area, preview, ScaleMode.ScaleToFit);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(item.IsPlaceholder ? "Save and Insert" : "Save", GUILayout.Height(28f)))
                Defer(() => SavePreview(item));

            if (GUILayout.Button("Discard", GUILayout.Height(28f), GUILayout.Width(80f)))
                Defer(ClearPreview);

            EditorGUILayout.EndHorizontal();
        }

        #endregion Selected Item

        #region Capture

        private void StartCapture()
        {
            ClearCapture();
            ClearPreview();

            pendingPanels.Add(leftPanel);

            if (twoPanels)
                pendingPanels.Add(rightPanel);

            foreach (PanelSettings panel in pendingPanels)
            {
                string problem = CheckPanel(panel);

                if (problem == null)
                    continue;

                SetStatus(problem, MessageType.Warning);
                ClearCapture();
                return;
            }

            isCapturing = true;
            PrepareNextPanel();
            EditorApplication.update += CaptureTick;
        }

        /// <summary>
        /// Brings the next window to the front and gives it time to redraw,
        /// because a window capture reads whatever is on screen.
        /// </summary>
        private void PrepareNextPanel()
        {
            PanelSettings panel = pendingPanels[capturedPanels.Count];

            if (panel.Source == PanelSource.EditorWindow)
            {
                EditorWindow window = FindWindow(panel.WindowKey);
                window.Focus();
                window.Repaint();
            }

            captureAt = EditorApplication.timeSinceStartup + SettleSeconds;
        }

        private void CaptureTick()
        {
            if (EditorApplication.timeSinceStartup < captureAt)
                return;

            try
            {
                capturedPanels.Add(CapturePanel(pendingPanels[capturedPanels.Count]));

                if (capturedPanels.Count < pendingPanels.Count)
                {
                    PrepareNextPanel();
                    return;
                }

                preview = capturedPanels.Count == 1
                    ? capturedPanels[0]
                    : PanelCapture.SideBySide(capturedPanels, PanelGap, PanelBackground);

                // The single panel became the preview, so it must not be destroyed with the parts.
                if (capturedPanels.Count == 1)
                    capturedPanels.Clear();

                previewSource = selectedSource;
                SetStatus("Check the preview, then save it or capture again.", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus($"Capture failed:\n{exception.Message}", MessageType.Error);
                Debug.LogException(exception);
            }

            EditorApplication.update -= CaptureTick;
            isCapturing = false;
            ClearCapture();
            Focus();
            Repaint();
        }

        private Texture2D CapturePanel(PanelSettings panel)
        {
            if (panel.Source == PanelSource.Camera)
                return PanelCapture.RenderCamera(panel.Camera, panel.Width, panel.Height);

            float scale = screenScaleOverride > 0f ? screenScaleOverride : detectedScreenScale;

            if (panel.Source == PanelSource.FullEditor)
                return PanelCapture.CaptureMainEditorWindow(scale, flipVertically);

            return PanelCapture.CaptureWindow(FindWindow(panel.WindowKey), panel.CropTopPoints, scale, flipVertically);
        }

        private string CheckPanel(PanelSettings panel)
        {
            if (panel.Source == PanelSource.Camera)
            {
                if (panel.Camera == null)
                    panel.Camera = Camera.main;

                return panel.Camera == null ? "Choose a camera for each camera panel." : null;
            }

            if (panel.Source == PanelSource.FullEditor)
                return null;

            return FindWindow(panel.WindowKey) == null
                ? "Choose an open window for each window panel. Press Refresh if you opened it after this window."
                : null;
        }

        private void SavePreview(LabMediaItem item)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(item.FullPath) ?? string.Empty);
                File.WriteAllBytes(item.FullPath, preview.EncodeToPNG());

                string saved = $"Saved {GetProjectRelativePath(item.FullPath)}";

                if (item.IsPlaceholder)
                {
                    bool replaced = document.ReplacePlaceholder(item, altText, Mathf.Min(displayWidth, preview.width));
                    saved += replaced ? " and inserted the image block." : ", but the placeholder was not found. Press Refresh.";
                }
                else
                {
                    bool updated = document.UpdateImageBlock(item, altText, Mathf.Min(displayWidth, preview.width));
                    saved += updated ? " and updated the image block." : ", but the image block was not found. Press Refresh.";
                }

                ClearPreview();
                RefreshDocuments();

                // Move on to the next placeholder; a recapture stays on its image.
                if (item.IsPlaceholder)
                    SelectFirstPlaceholder();

                SetStatus(saved, MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus($"Saving failed:\n{exception.Message}", MessageType.Error);
                Debug.LogException(exception);
            }
        }

        private void InsertExistingFile(LabMediaItem item)
        {
            int width = Mathf.Min(displayWidth, ReadImageWidth(item.FullPath));

            if (document.ReplacePlaceholder(item, altText, width))
            {
                RefreshDocuments();
                SelectFirstPlaceholder();
                SetStatus($"Inserted {item.FileName}.", MessageType.Info);
            }
            else
            {
                SetStatus("The placeholder was not found. Press Refresh.", MessageType.Warning);
            }
        }

        /// <summary>
        /// Replaces the matching placeholder when GIF Capture saves a GIF to
        /// its path. Saves to any other path are ignored.
        /// </summary>
        private void OnGifSaved(string savedPath, int width)
        {
            document?.Reload();
            LabMediaItem item = document?.FindPlaceholderFor(savedPath);

            if (item == null)
                return;

            string alt = item.Source == selectedSource ? altText : item.Description;

            if (!document.ReplacePlaceholder(item, alt, Mathf.Min(displayWidth, width)))
                return;

            RefreshDocuments();
            SelectFirstPlaceholder();
            SetStatus($"Inserted {item.FileName}.", MessageType.Info);
            Repaint();
        }

        #endregion Capture

        #region Helpers

        private void RefreshWindows()
        {
            openWindows.Clear();
            openWindows.AddRange(Resources.FindObjectsOfTypeAll<EditorWindow>()
                .Where(window => window != this && window is not GifCaptureWindow)
                .OrderBy(window => window.titleContent.text));

            windowLabels = openWindows
                .Select(window => $"{window.titleContent.text} ({window.GetType().Name})")
                .ToArray();
        }

        private static string KeyFor(EditorWindow window)
        {
            return $"{window.GetType().FullName}|{window.titleContent.text}";
        }

        /// <summary>
        /// Finds a window by type and title, falling back to type alone,
        /// because some windows change their title (for example a locked Inspector).
        /// </summary>
        private static EditorWindow FindWindow(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            string typeName = key.Split('|')[0];

            return windows.FirstOrDefault(window => KeyFor(window) == key)
                ?? windows.FirstOrDefault(window => window.GetType().FullName == typeName);
        }

        /// <summary>
        /// Reads an image's width: from the header for a GIF, which Unity
        /// cannot load, and by loading the file for a PNG or JPG.
        /// </summary>
        private static int ReadImageWidth(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);

            if (path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                return bytes.Length > 7 ? bytes[6] | (bytes[7] << 8) : 0;

            Texture2D texture = new(2, 2);

            try
            {
                return texture.LoadImage(bytes) ? texture.width : 0;
            }
            finally
            {
                DestroyImmediate(texture);
            }
        }

        private void ClearCapture()
        {
            foreach (Texture2D texture in capturedPanels)
            {
                if (texture != null)
                    DestroyImmediate(texture);
            }

            capturedPanels.Clear();
            pendingPanels.Clear();
        }

        private void ClearPreview()
        {
            if (preview != null)
                DestroyImmediate(preview);

            preview = null;
            previewSource = null;
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }

        /// <summary>
        /// Runs an action after this GUI pass, because changing which controls
        /// are drawn part-way through a pass makes IMGUI report layout errors.
        /// </summary>
        private void Defer(Action action)
        {
            EditorApplication.delayCall += () =>
            {
                action();
                Repaint();
            };
        }

        private static string GetProjectRootPath()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static string GetProjectRelativePath(string absolutePath)
        {
            string projectRoot = GetProjectRootPath().Replace('\\', '/');
            string normalisedPath = absolutePath.Replace('\\', '/');

            if (!normalisedPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                return normalisedPath;

            return normalisedPath.Substring(projectRoot.Length + 1);
        }

        #endregion Helpers
    }
}
