using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GD
{
    /// <summary>
    /// Creates simple 2D scene-information overlays at predefined screen positions.
    /// Useful for objectives, controls, instructions, and other scene guidance.
    /// </summary>
    public class SceneInfoOverlayTool : EditorWindow
    {
        private enum ScreenPosition
        {
            TopLeft,
            TopCentre,
            TopRight,
            MiddleLeft,
            Centre,
            MiddleRight,
            BottomLeft,
            BottomCentre,
            BottomRight
        }

        private const string CanvasName = "_SceneInfoCanvas";
        private const string PanelName = "Scene Info";

        private string overlayText =
            "Objective:\nDescribe the objective here.\n\nControls:\nWASD - Move";

        private ScreenPosition screenPosition = ScreenPosition.TopLeft;

        private float width = 420f;
        private float height = 160f;
        private float margin = 24f;
        private float fontSize = 24f;
        private float padding = 16f;

        private bool addBackground = true;
        private float backgroundAlpha = 0.65f;

        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.None;

        /// <summary>
        /// Opens the Scene Info Overlay tool.
        /// </summary>
        [MenuItem("Tools/GD/Setup/Create Scene Info Overlay", priority = 2)]
        private static void ShowWindow()
        {
            SceneInfoOverlayTool window =
                GetWindow<SceneInfoOverlayTool>();

            window.titleContent =
                new GUIContent("Scene Info Overlay");

            window.minSize =
                new Vector2(430f, 520f);

            window.Show();
        }

        /// <summary>
        /// Draws the editor window.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(8f);

            DrawTextSection();

            GUILayout.Space(8f);

            DrawLayoutSection();

            GUILayout.Space(8f);

            DrawAppearanceSection();

            GUILayout.FlexibleSpace();

            DrawStatus();
            DrawButtons();
        }

        /// <summary>
        /// Draws heading and guidance.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "Scene Info Overlay",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Create a simple on-screen text panel for scene objectives, controls, " +
                "instructions, or other guidance. The tool creates or reuses a dedicated " +
                "Screen Space Overlay Canvas.",
                MessageType.Info);
        }

        /// <summary>
        /// Draws the text-entry area.
        /// </summary>
        private void DrawTextSection()
        {
            EditorGUILayout.LabelField(
                "Text",
                EditorStyles.boldLabel);

            overlayText =
                EditorGUILayout.TextArea(
                    overlayText,
                    GUILayout.Height(150f));
        }

        /// <summary>
        /// Draws position and sizing controls.
        /// </summary>
        private void DrawLayoutSection()
        {
            EditorGUILayout.LabelField(
                "Layout",
                EditorStyles.boldLabel);

            screenPosition =
                (ScreenPosition)EditorGUILayout.EnumPopup(
                    "Position",
                    screenPosition);

            width =
                EditorGUILayout.FloatField(
                    "Width",
                    width);

            height =
                EditorGUILayout.FloatField(
                    "Height",
                    height);

            margin =
                EditorGUILayout.FloatField(
                    "Screen Margin",
                    margin);

            padding =
                EditorGUILayout.FloatField(
                    "Text Padding",
                    padding);
        }

        /// <summary>
        /// Draws text/background appearance controls.
        /// </summary>
        private void DrawAppearanceSection()
        {
            EditorGUILayout.LabelField(
                "Appearance",
                EditorStyles.boldLabel);

            fontSize =
                EditorGUILayout.FloatField(
                    "Font Size",
                    fontSize);

            addBackground =
                EditorGUILayout.Toggle(
                    "Background",
                    addBackground);

            using (new EditorGUI.DisabledScope(!addBackground))
            {
                backgroundAlpha =
                    EditorGUILayout.Slider(
                        "Background Opacity",
                        backgroundAlpha,
                        0f,
                        1f);
            }
        }

        /// <summary>
        /// Draws operation status.
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
        /// Draws the Create and Update buttons.
        /// </summary>
        private void DrawButtons()
        {
            bool valid =
                !string.IsNullOrWhiteSpace(overlayText) &&
                width > 0f &&
                height > 0f &&
                fontSize > 0f;

            GUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!valid))
            {
                if (GUILayout.Button(
                    "Create Overlay",
                    GUILayout.Height(30f)))
                {
                    CreateOverlay();
                }
            }

            bool canUpdate =
                GetSelectedOverlayPanel() != null;

            using (new EditorGUI.DisabledScope(!canUpdate || !valid))
            {
                if (GUILayout.Button(
                    "Update Selected",
                    GUILayout.Height(30f)))
                {
                    UpdateSelectedOverlay();
                }
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Creates a new scene info overlay.
        /// </summary>
        private void CreateOverlay()
        {
            ClearStatus();

            Canvas canvas =
                GetOrCreateCanvas();

            GameObject panel =
                CreatePanel(canvas.transform);

            ApplyOverlaySettings(panel);

            Selection.activeGameObject =
                panel;

            EditorGUIUtility.PingObject(panel);

            statusMessage =
                "Scene info overlay created.";

            statusType =
                MessageType.Info;
        }

        /// <summary>
        /// Updates the currently selected overlay panel.
        /// </summary>
        private void UpdateSelectedOverlay()
        {
            ClearStatus();

            GameObject panel =
                GetSelectedOverlayPanel();

            if (panel == null)
            {
                statusMessage =
                    "Select an existing Scene Info overlay panel first.";

                statusType =
                    MessageType.Warning;

                return;
            }

            Undo.RecordObject(
                panel.transform,
                "Update Scene Info Overlay");

            ApplyOverlaySettings(panel);

            statusMessage =
                "Selected scene info overlay updated.";

            statusType =
                MessageType.Info;
        }

        /// <summary>
        /// Creates or returns the dedicated overlay Canvas.
        /// </summary>
        private static Canvas GetOrCreateCanvas()
        {
            GameObject existing =
                GameObject.Find(CanvasName);

            if (existing != null)
            {
                Canvas existingCanvas =
                    existing.GetComponent<Canvas>();

                if (existingCanvas != null)
                    return existingCanvas;
            }

            GameObject canvasObject =
                new GameObject(
                    CanvasName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

            Undo.RegisterCreatedObjectUndo(
                canvasObject,
                "Create Scene Info Canvas");

            Canvas canvas =
                canvasObject.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder =
                1000;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(1920f, 1080f);

            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            scaler.matchWidthOrHeight =
                0.5f;

            return canvas;
        }

        /// <summary>
        /// Creates the panel and its TextMeshPro text object.
        /// </summary>
        private static GameObject CreatePanel(
            Transform parent)
        {
            GameObject panel =
                new GameObject(
                    PanelName,
                    typeof(RectTransform),
                    typeof(Image));

            Undo.RegisterCreatedObjectUndo(
                panel,
                "Create Scene Info Overlay");

            panel.transform.SetParent(
                parent,
                false);

            GameObject textObject =
                new GameObject(
                    "Text",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));

            textObject.transform.SetParent(
                panel.transform,
                false);

            return panel;
        }

        /// <summary>
        /// Applies the current tool settings to an overlay panel.
        /// </summary>
        private void ApplyOverlaySettings(
            GameObject panel)
        {
            RectTransform panelRect =
                panel.GetComponent<RectTransform>();

            Image background =
                panel.GetComponent<Image>();

            TextMeshProUGUI text =
                panel.GetComponentInChildren<TextMeshProUGUI>(true);

            if (panelRect == null ||
                background == null ||
                text == null)
            {
                statusMessage =
                    "The selected object is not a valid Scene Info overlay.";

                statusType =
                    MessageType.Error;

                return;
            }

            Undo.RecordObject(
                panelRect,
                "Update Scene Info Overlay");

            Undo.RecordObject(
                background,
                "Update Scene Info Overlay");

            Undo.RecordObject(
                text,
                "Update Scene Info Overlay");

            ApplyPosition(
                panelRect,
                screenPosition,
                margin);

            panelRect.sizeDelta =
                new Vector2(
                    width,
                    height);

            background.enabled =
                addBackground;

            background.color =
                new Color(
                    0f,
                    0f,
                    0f,
                    backgroundAlpha);

            ConfigureText(
                text,
                padding);

            text.text =
                overlayText;

            text.fontSize =
                fontSize;

            text.color =
                Color.white;

            text.overflowMode =
               TextOverflowModes.Ellipsis;

            text.alignment =
                GetTextAlignment(
                    screenPosition);

            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(text);
        }

        /// <summary>
        /// Configures the child text RectTransform to fill the panel with padding.
        /// </summary>
        private static void ConfigureText(
            TextMeshProUGUI text,
            float padding)
        {
            RectTransform textRect =
                text.rectTransform;

            textRect.anchorMin =
                Vector2.zero;

            textRect.anchorMax =
                Vector2.one;

            textRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            textRect.offsetMin =
                new Vector2(
                    padding,
                    padding);

            textRect.offsetMax =
                new Vector2(
                    -padding,
                    -padding);
        }

        /// <summary>
        /// Applies the selected anchor, pivot, and screen margin.
        /// </summary>
        private static void ApplyPosition(
            RectTransform rect,
            ScreenPosition position,
            float margin)
        {
            Vector2 anchor =
                GetAnchor(position);

            rect.anchorMin =
                anchor;

            rect.anchorMax =
                anchor;

            rect.pivot =
                anchor;

            rect.anchoredPosition =
                GetMarginOffset(
                    position,
                    margin);
        }

        /// <summary>
        /// Returns the anchor/pivot corresponding to a predefined screen position.
        /// </summary>
        private static Vector2 GetAnchor(
            ScreenPosition position)
        {
            return position switch
            {
                ScreenPosition.TopLeft =>
                    new Vector2(0f, 1f),

                ScreenPosition.TopCentre =>
                    new Vector2(0.5f, 1f),

                ScreenPosition.TopRight =>
                    new Vector2(1f, 1f),

                ScreenPosition.MiddleLeft =>
                    new Vector2(0f, 0.5f),

                ScreenPosition.Centre =>
                    new Vector2(0.5f, 0.5f),

                ScreenPosition.MiddleRight =>
                    new Vector2(1f, 0.5f),

                ScreenPosition.BottomLeft =>
                    new Vector2(0f, 0f),

                ScreenPosition.BottomCentre =>
                    new Vector2(0.5f, 0f),

                ScreenPosition.BottomRight =>
                    new Vector2(1f, 0f),

                _ =>
                    new Vector2(0f, 1f)
            };
        }

        /// <summary>
        /// Returns an inward screen-margin offset for the selected position.
        /// </summary>
        private static Vector2 GetMarginOffset(
            ScreenPosition position,
            float margin)
        {
            return position switch
            {
                ScreenPosition.TopLeft =>
                    new Vector2(margin, -margin),

                ScreenPosition.TopCentre =>
                    new Vector2(0f, -margin),

                ScreenPosition.TopRight =>
                    new Vector2(-margin, -margin),

                ScreenPosition.MiddleLeft =>
                    new Vector2(margin, 0f),

                ScreenPosition.Centre =>
                    Vector2.zero,

                ScreenPosition.MiddleRight =>
                    new Vector2(-margin, 0f),

                ScreenPosition.BottomLeft =>
                    new Vector2(margin, margin),

                ScreenPosition.BottomCentre =>
                    new Vector2(0f, margin),

                ScreenPosition.BottomRight =>
                    new Vector2(-margin, margin),

                _ =>
                    Vector2.zero
            };
        }

        /// <summary>
        /// Returns a sensible TextMeshPro alignment for the chosen screen position.
        /// </summary>
        private static TextAlignmentOptions GetTextAlignment(
            ScreenPosition position)
        {
            return position switch
            {
                ScreenPosition.TopLeft =>
                    TextAlignmentOptions.TopLeft,

                ScreenPosition.TopCentre =>
                    TextAlignmentOptions.Top,

                ScreenPosition.TopRight =>
                    TextAlignmentOptions.TopRight,

                ScreenPosition.MiddleLeft =>
                    TextAlignmentOptions.Left,

                ScreenPosition.Centre =>
                    TextAlignmentOptions.Center,

                ScreenPosition.MiddleRight =>
                    TextAlignmentOptions.Right,

                ScreenPosition.BottomLeft =>
                    TextAlignmentOptions.BottomLeft,

                ScreenPosition.BottomCentre =>
                    TextAlignmentOptions.Bottom,

                ScreenPosition.BottomRight =>
                    TextAlignmentOptions.BottomRight,

                _ =>
                    TextAlignmentOptions.TopLeft
            };
        }

        /// <summary>
        /// Returns the selected GameObject when it looks like an overlay panel
        /// produced by this tool.
        /// </summary>
        private static GameObject GetSelectedOverlayPanel()
        {
            GameObject selected =
                Selection.activeGameObject;

            if (selected == null)
                return null;

            if (selected.GetComponent<RectTransform>() == null)
                return null;

            if (selected.GetComponent<Image>() == null)
                return null;

            if (selected.GetComponentInChildren<TextMeshProUGUI>(true) == null)
                return null;

            Canvas parentCanvas =
                selected.GetComponentInParent<Canvas>();

            if (parentCanvas == null ||
                parentCanvas.gameObject.name != CanvasName)
            {
                return null;
            }

            return selected;
        }

        /// <summary>
        /// Clears the current tool status message.
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
