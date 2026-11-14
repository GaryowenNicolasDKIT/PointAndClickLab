using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GD
{
    /// <summary>
    /// Records a camera in Play Mode at a fixed frame rate, lets you trim the
    /// take and check its file size, then saves it as an animated GIF for use
    /// in notes and labs.
    /// </summary>
    public class GifCaptureWindow : EditorWindow
    {
        private const string DefaultFolder = "Docs/images";
        private const string GameViewMenuPath = "Window/General/Game";
        private const string WindowTitle = "GIF Capture";
        private const long FinalMessageMilliseconds = 2500;
        private const long PreviewMilliseconds = 3000;
        private const float MaxPreviewHeight = 300f;

        private static readonly Color CountdownColour = new Color(0.85f, 0.55f, 0.05f);
        private static readonly Color RecordingColour = new Color(0.80f, 0.10f, 0.10f);
        private static readonly Color SavedColour = new Color(0.15f, 0.55f, 0.20f);
        private static readonly Color ReviewColour = new Color(0.15f, 0.40f, 0.75f);

        [SerializeField] private Camera targetCamera;
        [SerializeField] private int width = 640;
        [SerializeField] private int height = 360;
        [SerializeField] private bool supersample = true;
        [SerializeField] private int framesPerSecond = 15;
        [SerializeField] private float maxDuration = 6f;
        [SerializeField] private float startDelay = 1f;
        [SerializeField] private bool recordOnPlay;
        [SerializeField] private bool hotkeyEnabled = true;
        [SerializeField] private Key hotkey = Key.F9;
        [SerializeField] private bool showKeys;
        [SerializeField] private OverlayPosition keysPosition = OverlayPosition.BottomLeft;
        [SerializeField] private bool showGameViewOverlay = true;
        [SerializeField] private OverlayPosition overlayPosition = OverlayPosition.TopRight;
        [SerializeField, Range(0.1f, 1f)] private float overlayOpacity = 0.85f;
        [SerializeField] private bool reviewBeforeSaving = true;
        [SerializeField] private int sizeBudgetKilobytes = 2048;
        [SerializeField] private bool dither;
        [SerializeField] private bool loop = true;
        [SerializeField] private string outputFolder = DefaultFolder;
        [SerializeField] private string fileName = "movement_capture";
        [SerializeField] private string altText = string.Empty;
        [SerializeField] private bool overwriteExisting;
        [SerializeField] private string lastSavedPath = string.Empty;
        [SerializeField] private int lastSavedWidth;

        // The take: captured frames (bottom-up, as Unity reads them) and the
        // game time of each. Not serialized, so a script reload discards it.
        private readonly List<Color32[]> frames = new List<Color32[]>();
        private readonly List<float> timestamps = new List<float>();

        private Camera recordingCamera;
        private RenderTexture renderTexture;
        private Texture2D readback;
        private int captureWidth;
        private int captureHeight;
        private int captureFramesPerSecond;
        private bool isRecording;
        private float startTime;
        private float nextCaptureTime;
        private bool hotkeyWasDown;

        // Review state.
        private bool isReviewing;
        private float inFrame;
        private float outFrame;
        private int frameStep = 1;
        private bool halfResolution;
        private int previewIndex;
        private int shownPreviewIndex = -1;
        private Texture2D previewTexture;
        private bool previewPlaying;
        private double nextPreviewTime;

        // The last encoding, kept so Save can reuse the size estimate's bytes.
        private byte[] encodedBytes;
        private string encodedSignature;
        private int encodedWidth;
        private int encodedHeight;
        private int encodedFrameCount;
        private int encodedColourCount;

        // Created on first use: Unity does not always run field initialisers
        // when it restores an open window after a script reload.
        private GameViewRecordingOverlay overlay;
        private GameViewRecordingOverlay Overlay => overlay ??= new GameViewRecordingOverlay();
        private EditorWindow gameView;

        private Vector2 scroll;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.None;

        /// <summary>
        /// Opens the GIF Capture window.
        /// </summary>
        [MenuItem("Tools/GD/Evidence/GIF Capture", priority = 301)]
        private static void ShowWindow()
        {
            GifCaptureWindow window = GetWindow<GifCaptureWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(420f, 480f);
            window.Show();
        }

        /// <summary>
        /// Raised after a GIF is saved, with its full path and pixel width.
        /// </summary>
        public static event Action<string, int> Saved;

        /// <summary>
        /// Opens the window ready to record a GIF to a fixed path, replacing
        /// any file already there. Used by Lab Media Capture, so a lab's GIF
        /// placeholder is filled without retyping its path.
        /// </summary>
        /// <param name="folder">Project-relative folder, for example Docs/images/labs.</param>
        /// <param name="gifFileName">File name without the .gif extension.</param>
        /// <param name="gifAltText">Alt text for the image block.</param>
        public static void OpenFor(string folder, string gifFileName, string gifAltText)
        {
            ShowWindow();

            GifCaptureWindow window = GetWindow<GifCaptureWindow>();
            window.outputFolder = folder;
            window.fileName = gifFileName;
            window.altText = gifAltText;
            window.overwriteExisting = true;
            window.SetStatus($"Ready to record {folder}/{gifFileName}.gif", MessageType.Info);
            window.Repaint();
        }

        private void OnEnable()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // A script reload drops the reference to any overlay still shown.
            GameViewRecordingOverlay.RemoveStrayOverlays();
            hotkeyWasDown = false;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            Overlay.Hide();
            isRecording = false;
            ReleaseCaptureTargets();
            ClearTake();
        }

        /// <summary>
        /// Starts recording on entering Play Mode if requested, saves or reviews
        /// a recording still running when Play Mode ends, and protects an
        /// unsaved take from the script reload that entering Play Mode causes.
        /// </summary>
        private void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode when isReviewing:
                    ResolveUnsavedTake();
                    break;

                case PlayModeStateChange.EnteredPlayMode when recordOnPlay:
                    StartRecording();
                    break;

                case PlayModeStateChange.ExitingPlayMode when isRecording:
                    StopRecording(true);
                    break;
            }
        }

        private void ResolveUnsavedTake()
        {
            int choice = EditorUtility.DisplayDialogComplex(
                WindowTitle,
                "You have an unsaved take. Entering Play Mode discards it.",
                "Save GIF",
                "Cancel Play Mode",
                "Discard Take");

            switch (choice)
            {
                case 0:
                    SaveSelection();
                    ClearTake();
                    break;

                case 1:
                    EditorApplication.isPlaying = false;
                    break;

                default:
                    ClearTake();
                    break;
            }
        }

        // -----------------------------------------------------------------
        // GUI
        // -----------------------------------------------------------------

        private void OnGUI()
        {
            if (isRecording)
            {
                DrawBanner(BuildOverlayText(), Time.unscaledTime < startTime ? CountdownColour : RecordingColour);
            }
            else if (isReviewing)
            {
                DrawBanner("REVIEW - trim the take, check its size, then save", ReviewColour);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("GIF Capture", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Records a camera in Play Mode and saves a looping GIF. " +
                "Keep clips short and small: 3-6 seconds at 640 x 360 and 15 fps " +
                "usually produces a file under 2 MB. Hover over a field for details.",
                MessageType.Info);

            if (isReviewing)
            {
                GUILayout.Space(6f);
                DrawReviewPanel();
                GUILayout.Space(6f);
                DrawOutputSettings();
            }
            else
            {
                using (new EditorGUI.DisabledScope(isRecording))
                {
                    GUILayout.Space(6f);
                    DrawSourceSettings();
                    GUILayout.Space(6f);
                    DrawTimingSettings();
                    GUILayout.Space(6f);
                    DrawKeyDisplaySettings();
                    GUILayout.Space(6f);
                    DrawOverlaySettings();
                    GUILayout.Space(6f);
                    DrawOutputSettings();
                }
            }

            GUILayout.Space(10f);
            DrawStatus();

            if (!isReviewing)
            {
                DrawRecordButton();
                DrawResultButtons();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws a full-width coloured bar so the window's state is visible at a glance.
        /// </summary>
        private static void DrawBanner(string text, Color colour)
        {
            Rect bar = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, colour);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15
            };
            style.normal.textColor = Color.white;

            GUI.Label(bar, text, style);
        }

        private void DrawSourceSettings()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);

            targetCamera = (Camera)EditorGUILayout.ObjectField(
                Labels.Camera,
                targetCamera,
                typeof(Camera),
                true);

            width = Mathf.Clamp(EditorGUILayout.IntField(Labels.Width, width), 16, 1920);
            height = Mathf.Clamp(EditorGUILayout.IntField(Labels.Height, height), 16, 1080);

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);

            if (GUILayout.Button(Labels.Size480))
            {
                SetSize(480, 270);
            }

            if (GUILayout.Button(Labels.Size640))
            {
                SetSize(640, 360);
            }

            if (GUILayout.Button(Labels.Size800))
            {
                SetSize(800, 450);
            }

            GUILayout.EndHorizontal();

            supersample = EditorGUILayout.Toggle(Labels.Supersample, supersample);
        }

        private void DrawTimingSettings()
        {
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);

            framesPerSecond = EditorGUILayout.IntSlider(Labels.FramesPerSecond, framesPerSecond, 5, 30);
            maxDuration = EditorGUILayout.Slider(Labels.MaxDuration, maxDuration, 1f, 30f);
            startDelay = EditorGUILayout.Slider(Labels.StartDelay, startDelay, 0f, 5f);
            recordOnPlay = EditorGUILayout.Toggle(Labels.RecordOnPlay, recordOnPlay);

            hotkeyEnabled = EditorGUILayout.Toggle(Labels.HotkeyEnabled, hotkeyEnabled);

            using (new EditorGUI.DisabledScope(!hotkeyEnabled))
            {
                hotkey = (Key)EditorGUILayout.EnumPopup(Labels.Hotkey, hotkey);
            }

            int maxFrames = Mathf.CeilToInt(maxDuration * framesPerSecond);
            float megabytes = maxFrames * width * height * 4f / (1024f * 1024f);

            EditorGUILayout.LabelField(
                $"Up to {maxFrames} frames, about {megabytes:0} MB of memory while recording.",
                EditorStyles.miniLabel);
        }

        private void DrawKeyDisplaySettings()
        {
            EditorGUILayout.LabelField("Key Display", EditorStyles.boldLabel);

            showKeys = EditorGUILayout.Toggle(Labels.ShowKeys, showKeys);

            using (new EditorGUI.DisabledScope(!showKeys))
            {
                keysPosition = (OverlayPosition)EditorGUILayout.EnumPopup(Labels.KeysPosition, keysPosition);
            }
        }

        private void DrawOverlaySettings()
        {
            EditorGUILayout.LabelField("Game View Overlay", EditorStyles.boldLabel);

            showGameViewOverlay = EditorGUILayout.Toggle(Labels.ShowOverlay, showGameViewOverlay);

            using (new EditorGUI.DisabledScope(!showGameViewOverlay))
            {
                EditorGUI.BeginChangeCheck();

                overlayPosition = (OverlayPosition)EditorGUILayout.EnumPopup(Labels.OverlayPosition, overlayPosition);
                overlayOpacity = EditorGUILayout.Slider(Labels.OverlayOpacity, overlayOpacity, 0.1f, 1f);

                // Restyle a visible preview straight away, so the effect of each
                // change can be judged without pressing Preview again.
                if (EditorGUI.EndChangeCheck() && Overlay.IsShown)
                {
                    Overlay.UpdateAppearance(overlayPosition, overlayOpacity, PreviewMilliseconds);
                    GameViewRecordingOverlay.FindGameView()?.Repaint();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);

                if (GUILayout.Button(Labels.PreviewOverlay))
                {
                    PreviewOverlay();
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawOutputSettings()
        {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(isReviewing))
            {
                reviewBeforeSaving = EditorGUILayout.Toggle(Labels.ReviewBeforeSaving, reviewBeforeSaving);
            }

            sizeBudgetKilobytes = Mathf.Max(50, EditorGUILayout.IntField(Labels.SizeBudget, sizeBudgetKilobytes));
            dither = EditorGUILayout.Toggle(Labels.Dither, dither);
            loop = EditorGUILayout.Toggle(Labels.Loop, loop);

            GUILayout.BeginHorizontal();
            outputFolder = EditorGUILayout.TextField(Labels.OutputFolder, outputFolder);

            if (GUILayout.Button(Labels.Browse, GUILayout.Width(64f)))
            {
                BrowseForOutputFolder();
            }

            GUILayout.EndHorizontal();

            fileName = EditorGUILayout.TextField(Labels.FileName, fileName);
            overwriteExisting = EditorGUILayout.Toggle(Labels.OverwriteExisting, overwriteExisting);

            EditorGUILayout.LabelField(Labels.AltText);
            altText = EditorGUILayout.TextArea(altText, GUILayout.Height(40f));
        }

        /// <summary>
        /// Shows one frame of the take, a scrub bar and playback, the trim
        /// range, the frame-rate and resolution options and the size estimate.
        /// </summary>
        private void DrawReviewPanel()
        {
            EditorGUILayout.LabelField("Review", EditorStyles.boldLabel);

            int lastIndex = frames.Count - 1;
            int first = TrimFirst;
            int last = TrimLast;

            DrawPreviewImage(first, last);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(previewPlaying ? Labels.Pause : Labels.Play, GUILayout.Width(64f)))
            {
                TogglePreviewPlayback();
            }

            previewIndex = EditorGUILayout.IntSlider(previewIndex, 0, lastIndex);
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                $"Frame {previewIndex + 1} of {frames.Count}, at {timestamps[previewIndex]:0.00} s",
                EditorStyles.miniLabel);

            GUILayout.Space(6f);

            EditorGUILayout.MinMaxSlider(Labels.Trim, ref inFrame, ref outFrame, 0f, lastIndex);

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);

            if (GUILayout.Button(Labels.SetIn))
            {
                inFrame = previewIndex;
                outFrame = Mathf.Max(outFrame, inFrame);
            }

            if (GUILayout.Button(Labels.SetOut))
            {
                outFrame = previewIndex;
                inFrame = Mathf.Min(inFrame, outFrame);
            }

            if (GUILayout.Button(Labels.ResetTrim))
            {
                inFrame = 0f;
                outFrame = lastIndex;
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                $"Keeps frames {first + 1}-{last + 1}: {SelectionDuration():0.0} s",
                EditorStyles.miniLabel);

            GUILayout.Space(6f);

            frameStep = EditorGUILayout.IntPopup(
                Labels.FrameStep,
                frameStep,
                new[]
                {
                    new GUIContent($"Every frame ({captureFramesPerSecond} fps)"),
                    new GUIContent($"Every 2nd frame ({captureFramesPerSecond / 2f:0.#} fps)"),
                    new GUIContent($"Every 3rd frame ({captureFramesPerSecond / 3f:0.#} fps)")
                },
                new[] { 1, 2, 3 });

            halfResolution = EditorGUILayout.Toggle(
                new GUIContent(
                    $"Half Resolution ({captureWidth / 2} x {captureHeight / 2})",
                    Labels.HalfResolution.tooltip),
                halfResolution);

            GUILayout.Space(6f);
            DrawSizeEstimate();

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(Labels.SaveTake, GUILayout.Height(32f)))
            {
                Defer(SaveTakeAndClose);
            }

            if (GUILayout.Button(Labels.DiscardTake, GUILayout.Height(32f), GUILayout.Width(110f)))
            {
                Defer(DiscardTake);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawPreviewImage(int first, int last)
        {
            UpdatePreviewTexture();

            float aspect = captureHeight / (float)captureWidth;
            float availableWidth = EditorGUIUtility.currentViewWidth - 30f;
            float previewHeight = Mathf.Min(availableWidth * aspect, MaxPreviewHeight);

            Rect rect = GUILayoutUtility.GetRect(availableWidth, previewHeight, GUILayout.ExpandWidth(true));

            if (previewTexture != null)
            {
                GUI.DrawTexture(rect, previewTexture, ScaleMode.ScaleToFit);
            }

            // Dim frames that the trim will cut, so it is obvious they are not kept.
            if (previewIndex < first || previewIndex > last)
            {
                EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.6f));

                GUIStyle style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                style.normal.textColor = Color.white;
                GUI.Label(rect, "Outside the trim - not saved", style);
            }
        }

        /// <summary>
        /// Shows the size of the last encoding against the budget, and offers
        /// the two changes that shrink a GIF most when it is over budget.
        /// </summary>
        private void DrawSizeEstimate()
        {
            if (!IsEncodingCurrent())
            {
                EditorGUILayout.HelpBox("The trim or output settings changed since the last estimate.", MessageType.None);

                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);

                if (GUILayout.Button(Labels.EstimateSize))
                {
                    Defer(() => TryEncode());
                }

                GUILayout.EndHorizontal();
                return;
            }

            long kilobytes = encodedBytes.Length / 1024;

            EditorGUILayout.LabelField(
                Labels.EstimatedSize,
                new GUIContent($"{kilobytes} KB of {sizeBudgetKilobytes} KB budget ({encodedWidth} x {encodedHeight}, {encodedFrameCount} frames)"));

            if (kilobytes <= sizeBudgetKilobytes)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                $"Over budget by {kilobytes - sizeBudgetKilobytes} KB. Trim the take, " +
                "or use one of the options below. Halving the resolution roughly quarters the size; " +
                "halving the frame rate roughly halves it.",
                MessageType.Warning);

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);

            using (new EditorGUI.DisabledScope(frameStep >= 3))
            {
                if (GUILayout.Button(Labels.FewerFrames))
                {
                    frameStep = Mathf.Min(3, frameStep + 1);
                    Defer(() => TryEncode());
                }
            }

            using (new EditorGUI.DisabledScope(halfResolution))
            {
                if (GUILayout.Button(Labels.HalveResolution))
                {
                    halfResolution = true;
                    Defer(() => TryEncode());
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawStatus()
        {
            // While recording, the banner at the top shows the state instead.
            if (isRecording || string.IsNullOrEmpty(statusMessage))
            {
                return;
            }

            EditorGUILayout.HelpBox(statusMessage, statusType);
            GUILayout.Space(4f);
        }

        private void DrawRecordButton()
        {
            if (isRecording)
            {
                if (GUILayout.Button(Labels.Stop, GUILayout.Height(32f)))
                {
                    Defer(() => StopRecording(true));
                }

                return;
            }

            if (!EditorApplication.isPlaying)
            {
                string hotkeyHint = hotkeyEnabled ? $" or press {hotkey}" : string.Empty;

                EditorGUILayout.HelpBox(
                    recordOnPlay
                        ? "Enter Play Mode to start recording."
                        : $"Enter Play Mode, then press Record{hotkeyHint}.",
                    MessageType.None);
            }

            using (new EditorGUI.DisabledScope(!CanStartRecording()))
            {
                if (GUILayout.Button(Labels.Record, GUILayout.Height(32f)))
                {
                    Defer(StartRecording);
                }
            }
        }

        private void DrawResultButtons()
        {
            if (isRecording || string.IsNullOrEmpty(lastSavedPath) || !File.Exists(lastSavedPath))
            {
                return;
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(Labels.ShowInExplorer))
            {
                EditorUtility.RevealInFinder(lastSavedPath);
            }

            if (GUILayout.Button(Labels.CopyImageBlock))
            {
                EditorGUIUtility.systemCopyBuffer = BuildImageBlock(lastSavedPath);
                SetStatus("HTML image block copied to the clipboard.", MessageType.Info);
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Runs an action after this GUI pass. Starting, stopping or saving
        /// changes which controls the window draws, and changing them in the
        /// middle of a pass makes IMGUI report layout errors.
        /// </summary>
        private void Defer(Action action)
        {
            EditorApplication.delayCall += () =>
            {
                action();
                Repaint();
            };
        }

        // -----------------------------------------------------------------
        // Game view overlay
        // -----------------------------------------------------------------

        /// <summary>
        /// Text shared by the window banner and the Game view overlay.
        /// </summary>
        private string BuildOverlayText()
        {
            float now = Time.unscaledTime;

            if (now < startTime)
            {
                return $"Recording in {Mathf.CeilToInt(startTime - now)}";
            }

            string paused = EditorApplication.isPaused ? " - PAUSED" : string.Empty;

            return $"REC  {Mathf.FloorToInt(now - startTime)} / {Mathf.RoundToInt(maxDuration)} s{paused}";
        }

        /// <summary>
        /// Shows the countdown and recording state over the Game view.
        /// </summary>
        private void UpdateGameViewOverlay()
        {
            if (!showGameViewOverlay)
            {
                return;
            }

            Color background = Time.unscaledTime < startTime ? CountdownColour : RecordingColour;
            Overlay.Show(gameView, BuildOverlayText(), background, overlayPosition, overlayOpacity);
        }

        /// <summary>
        /// Shows a final message over the Game view, then removes the overlay.
        /// </summary>
        private void ShowFinalOverlay(string text, Color background)
        {
            if (showGameViewOverlay)
            {
                Overlay.ShowThenHide(
                    gameView != null ? gameView : GameViewRecordingOverlay.FindGameView(),
                    text,
                    background,
                    overlayPosition,
                    overlayOpacity,
                    FinalMessageMilliseconds);
            }
            else
            {
                Overlay.Hide();
            }

            gameView = null;
        }

        /// <summary>
        /// Shows a sample overlay so the position and opacity can be checked
        /// before recording.
        /// </summary>
        private void PreviewOverlay()
        {
            EditorWindow view = GameViewRecordingOverlay.FindGameView();

            if (view == null)
            {
                SetStatus("Open the Game view to preview the overlay.", MessageType.Warning);
                return;
            }

            Overlay.ShowThenHide(view, "REC  0 / 6 s (preview)", RecordingColour,
                overlayPosition, overlayOpacity, PreviewMilliseconds);
            view.Repaint();
        }

        // -----------------------------------------------------------------
        // Recording
        // -----------------------------------------------------------------

        private bool CanStartRecording()
        {
            return
                EditorApplication.isPlaying &&
                !isRecording &&
                !isReviewing &&
                !string.IsNullOrWhiteSpace(outputFolder) &&
                !string.IsNullOrWhiteSpace(fileName);
        }

        /// <summary>
        /// Locks the capture settings and creates the render targets.
        /// Capture itself begins after the start delay.
        /// </summary>
        private void StartRecording()
        {
            if (!CanStartRecording())
            {
                return;
            }

            recordingCamera = targetCamera != null ? targetCamera : Camera.main;

            if (recordingCamera == null)
            {
                SetStatus("No camera assigned and no camera tagged MainCamera in the scene.", MessageType.Error);
                return;
            }

            captureWidth = width;
            captureHeight = height;
            captureFramesPerSecond = framesPerSecond;

            int scale = supersample ? 2 : 1;

            renderTexture = new RenderTexture(
                captureWidth * scale,
                captureHeight * scale,
                24,
                RenderTextureFormat.ARGB32);

            readback = new Texture2D(
                captureWidth * scale,
                captureHeight * scale,
                TextureFormat.RGBA32,
                false);

            ClearTake();

            startTime = Time.unscaledTime + startDelay;
            nextCaptureTime = startTime;
            isRecording = true;

            ClearStatus();
            titleContent = new GUIContent($"{WindowTitle} - REC");

            // Return keyboard focus to the Game view so the player can be
            // controlled straight away, and keep it for the overlay.
            EditorApplication.ExecuteMenuItem(GameViewMenuPath);
            gameView = GameViewRecordingOverlay.FindGameView();
            UpdateGameViewOverlay();
        }

        /// <summary>
        /// Runs every Editor update: polls the hotkey, plays the review
        /// preview, and captures frames at the requested rate while recording.
        /// Capture uses unscaled game time so playback speed matches what the
        /// player saw.
        /// </summary>
        private void Tick()
        {
            PollHotkey();

            if (isReviewing)
            {
                TickPreviewPlayback();
                return;
            }

            if (!isRecording)
            {
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                StopRecording(true);
                return;
            }

            Repaint();
            UpdateGameViewOverlay();

            if (EditorApplication.isPaused)
            {
                return;
            }

            float now = Time.unscaledTime;

            if (now < nextCaptureTime)
            {
                return;
            }

            if (recordingCamera == null)
            {
                SetStatus("The recording camera was destroyed.", MessageType.Warning);
                StopRecording(true);
                return;
            }

            CaptureFrame(now - startTime);

            float interval = 1f / captureFramesPerSecond;
            nextCaptureTime += interval;

            // If the game stalls, skip ahead rather than capturing a burst.
            if (nextCaptureTime <= now)
            {
                nextCaptureTime = now + interval;
            }

            if (now - startTime >= maxDuration)
            {
                StopRecording(true);
            }
        }

        /// <summary>
        /// Starts or stops recording when the hotkey goes down. The Input
        /// System only receives keys while the Game view has focus, which is
        /// where you are while recording.
        /// </summary>
        private void PollHotkey()
        {
            if (!hotkeyEnabled || !EditorApplication.isPlaying || hotkey == Key.None)
            {
                hotkeyWasDown = false;
                return;
            }

            bool down = IsHotkeyDown();
            bool pressed = down && !hotkeyWasDown;
            hotkeyWasDown = down;

            if (!pressed)
            {
                return;
            }

            if (isRecording)
            {
                StopRecording(true);
            }
            else if (isReviewing)
            {
                ShowFinalOverlay("Save or discard the take in the GIF Capture window first", ReviewColour);
            }
            else
            {
                StartRecording();
            }

            Repaint();
        }

        private bool IsHotkeyDown()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return false;
            }

            try
            {
                return keyboard[hotkey].isPressed;
            }
            catch (ArgumentOutOfRangeException)
            {
                // A few Key values, such as IMESelected, are not physical keys.
                return false;
            }
        }

        private void CaptureFrame(float time)
        {
            RenderTexture previousTarget = recordingCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                recordingCamera.targetTexture = renderTexture;
                recordingCamera.Render();

                RenderTexture.active = renderTexture;
                readback.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
            }
            finally
            {
                recordingCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
            }

            Color32[] pixels = readback.GetPixels32();

            if (renderTexture.width != captureWidth)
            {
                pixels = Downsample(pixels, renderTexture.width, renderTexture.height);
            }

            // Drawn after downsampling so the keys are sharp at the GIF's size.
            if (showKeys)
            {
                KeyDisplay.Draw(pixels, captureWidth, captureHeight, KeyDisplay.Read(), keysPosition);
            }

            frames.Add(pixels);
            timestamps.Add(time);
        }

        /// <summary>
        /// Halves an image by averaging each 2x2 block into one pixel.
        /// </summary>
        private static Color32[] Downsample(Color32[] source, int sourceWidth, int sourceHeight)
        {
            int targetWidth = sourceWidth / 2;
            int targetHeight = sourceHeight / 2;
            Color32[] result = new Color32[targetWidth * targetHeight];

            for (int y = 0; y < targetHeight; y++)
            {
                int row0 = y * 2 * sourceWidth;
                int row1 = row0 + sourceWidth;

                for (int x = 0; x < targetWidth; x++)
                {
                    Color32 a = source[row0 + x * 2];
                    Color32 b = source[row0 + x * 2 + 1];
                    Color32 c = source[row1 + x * 2];
                    Color32 d = source[row1 + x * 2 + 1];

                    result[y * targetWidth + x] = new Color32(
                        (byte)((a.r + b.r + c.r + d.r + 2) >> 2),
                        (byte)((a.g + b.g + c.g + d.g + 2) >> 2),
                        (byte)((a.b + b.b + c.b + d.b + 2) >> 2),
                        255);
                }
            }

            return result;
        }

        /// <summary>
        /// Ends a recording. With review on, the take opens for trimming;
        /// otherwise the whole take is saved straight away.
        /// </summary>
        private void StopRecording(bool save)
        {
            isRecording = false;
            titleContent = new GUIContent(WindowTitle);
            ReleaseCaptureTargets();

            if (!save || frames.Count == 0)
            {
                if (save)
                {
                    SetStatus("Recording stopped before any frames were captured.", MessageType.Warning);
                }

                ShowFinalOverlay("Recording stopped", CountdownColour);
                ClearTake();
                return;
            }

            inFrame = 0f;
            outFrame = frames.Count - 1;
            frameStep = 1;
            halfResolution = false;

            if (reviewBeforeSaving)
            {
                EnterReview();
                ShowFinalOverlay($"Recorded {frames.Count} frames - review in the GIF Capture window", ReviewColour);
                return;
            }

            ShowFinalOverlay(
                SaveSelection() ? $"Saved {Path.GetFileName(lastSavedPath)}" : "Saving failed - see the GIF Capture window",
                SavedColour);

            ClearTake();
            Repaint();
        }

        // -----------------------------------------------------------------
        // Review
        // -----------------------------------------------------------------

        private int TrimFirst => Mathf.Clamp(Mathf.RoundToInt(inFrame), 0, frames.Count - 1);
        private int TrimLast => Mathf.Clamp(Mathf.RoundToInt(outFrame), TrimFirst, frames.Count - 1);

        private void EnterReview()
        {
            isReviewing = true;
            previewIndex = 0;
            shownPreviewIndex = -1;
            previewPlaying = false;

            previewTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };

            // Estimate straight away, so the size is visible before any trimming.
            TryEncode();

            Focus();
            Repaint();
        }

        private void UpdatePreviewTexture()
        {
            if (previewTexture == null || previewIndex == shownPreviewIndex || frames.Count == 0)
            {
                return;
            }

            // Both use bottom-up rows, so the frame can be copied as it is.
            previewTexture.SetPixels32(frames[previewIndex]);
            previewTexture.Apply(false);
            shownPreviewIndex = previewIndex;
        }

        private void TogglePreviewPlayback()
        {
            previewPlaying = !previewPlaying;

            if (previewPlaying && (previewIndex < TrimFirst || previewIndex >= TrimLast))
            {
                previewIndex = TrimFirst;
            }

            nextPreviewTime = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// Plays the trimmed range at the speed it will play in the GIF,
        /// including the frame step, and loops it.
        /// </summary>
        private void TickPreviewPlayback()
        {
            if (!previewPlaying)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;

            if (now < nextPreviewTime)
            {
                return;
            }

            previewIndex += frameStep;

            if (previewIndex > TrimLast || previewIndex < TrimFirst)
            {
                previewIndex = TrimFirst;
            }

            nextPreviewTime = now + frameStep / (double)captureFramesPerSecond;
            Repaint();
        }

        private float SelectionDuration()
        {
            return timestamps[TrimLast] - timestamps[TrimFirst] + frameStep / (float)captureFramesPerSecond;
        }

        private void SaveTakeAndClose()
        {
            if (SaveSelection())
            {
                ShowFinalOverlay($"Saved {Path.GetFileName(lastSavedPath)}", SavedColour);
                ClearTake();
            }
        }

        private void DiscardTake()
        {
            ClearTake();
            SetStatus("Take discarded.", MessageType.Info);
        }

        /// <summary>
        /// Frees the take and leaves review. Safe to call when there is no take.
        /// </summary>
        private void ClearTake()
        {
            frames.Clear();
            timestamps.Clear();
            isReviewing = false;
            previewPlaying = false;
            shownPreviewIndex = -1;
            encodedBytes = null;
            encodedSignature = null;

            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
        }

        // -----------------------------------------------------------------
        // Encoding and saving
        // -----------------------------------------------------------------

        /// <summary>
        /// Everything that changes the encoded bytes. If it matches the last
        /// encoding, those bytes can be saved as they are.
        /// </summary>
        private string CurrentSignature()
        {
            return $"{TrimFirst}|{TrimLast}|{frameStep}|{halfResolution}|{dither}|{loop}|{frames.Count}";
        }

        private bool IsEncodingCurrent()
        {
            return encodedBytes != null && encodedSignature == CurrentSignature();
        }

        private List<int> SelectedFrameIndices()
        {
            List<int> indices = new List<int>();

            for (int i = TrimFirst; i <= TrimLast; i += frameStep)
            {
                indices.Add(i);
            }

            return indices;
        }

        /// <summary>
        /// Encodes the trimmed selection, showing the error in the window if it fails.
        /// </summary>
        private bool TryEncode()
        {
            try
            {
                Encode();
                return true;
            }
            catch (Exception exception)
            {
                SetStatus($"Encoding failed:\n{exception.Message}", MessageType.Error);
                Debug.LogException(exception);
                return false;
            }
        }

        /// <summary>
        /// Quantises the selected frames to a shared palette and encodes them,
        /// keeping the bytes for the size estimate and for saving.
        /// </summary>
        private void Encode()
        {
            const string progressTitle = "GIF Capture";

            try
            {
                List<int> indices = SelectedFrameIndices();
                int outputWidth = halfResolution ? captureWidth / 2 : captureWidth;
                int outputHeight = halfResolution ? captureHeight / 2 : captureHeight;

                List<Color32[]> selected = new List<Color32[]>(indices.Count);

                foreach (int index in indices)
                {
                    selected.Add(halfResolution
                        ? Downsample(frames[index], captureWidth, captureHeight)
                        : frames[index]);
                }

                EditorUtility.DisplayProgressBar(progressTitle, "Building palette", 0f);
                GifPalette palette = GifPalette.Build(selected);

                List<byte[]> indexedFrames = new List<byte[]>(selected.Count);

                for (int i = 0; i < selected.Count; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        progressTitle,
                        $"Mapping frame {i + 1} of {selected.Count}",
                        0.2f + 0.4f * i / selected.Count);

                    indexedFrames.Add(palette.ToIndices(selected[i], outputWidth, outputHeight, dither));
                }

                encodedBytes = GifWriter.Encode(
                    outputWidth,
                    outputHeight,
                    palette,
                    indexedFrames,
                    BuildDelays(indices),
                    loop,
                    progress => EditorUtility.DisplayProgressBar(
                        progressTitle,
                        "Compressing frames",
                        0.6f + 0.4f * progress));

                encodedSignature = CurrentSignature();
                encodedWidth = outputWidth;
                encodedHeight = outputHeight;
                encodedFrameCount = indices.Count;
                encodedColourCount = palette.ColourCount;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Writes the selection to disk, reusing the size estimate's bytes
        /// when nothing has changed since. Returns false if saving failed.
        /// </summary>
        private bool SaveSelection()
        {
            try
            {
                if (!IsEncodingCurrent())
                {
                    Encode();
                }

                string path = BuildOutputPath();
                File.WriteAllBytes(path, encodedBytes);

                lastSavedPath = path;
                lastSavedWidth = encodedWidth;

                long kilobytes = encodedBytes.Length / 1024;
                bool overBudget = kilobytes > sizeBudgetKilobytes;

                SetStatus(
                    $"Saved {GetProjectRelativePath(path)}\n" +
                    $"{encodedFrameCount} frames, {SelectionDuration():0.0} s, {encodedWidth} x {encodedHeight}, " +
                    $"{encodedColourCount} colours, {kilobytes} KB" +
                    (overBudget ? $"\nOver the {sizeBudgetKilobytes} KB budget: turn on Review Before Saving to trim or shrink it." : string.Empty),
                    overBudget ? MessageType.Warning : MessageType.Info);

                RaiseSaved(path, encodedWidth);

                return true;
            }
            catch (Exception exception)
            {
                SetStatus($"Saving failed:\n{exception.Message}", MessageType.Error);
                Debug.LogException(exception);
                return false;
            }
        }

        /// <summary>
        /// Converts the capture times of the selected frames into delays in
        /// hundredths of a second. Rounding the running total, not each gap,
        /// stops drift. Frames skipped by the frame step add their time to
        /// the frame before, so the GIF plays at the original speed.
        /// </summary>
        private int[] BuildDelays(List<int> indices)
        {
            int[] delays = new int[indices.Count];

            for (int i = 0; i < indices.Count - 1; i++)
            {
                int gap =
                    Mathf.RoundToInt(timestamps[indices[i + 1]] * 100f) -
                    Mathf.RoundToInt(timestamps[indices[i]] * 100f);

                // Browsers replace delays below 2 with 10, which would slow the clip.
                delays[i] = Mathf.Max(2, gap);
            }

            delays[delays.Length - 1] = Mathf.Max(2, Mathf.RoundToInt(100f * frameStep / captureFramesPerSecond));

            return delays;
        }

        /// <summary>
        /// Builds the output path, adding a number rather than overwriting an
        /// existing GIF.
        /// </summary>
        private string BuildOutputPath()
        {
            string folder = Path.GetFullPath(Path.Combine(GetProjectRootPath(), outputFolder.Trim()));
            Directory.CreateDirectory(folder);

            string safeName = Path.GetFileNameWithoutExtension(fileName.Trim()).Replace(' ', '_');

            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidCharacter, '_');
            }

            string path = Path.Combine(folder, $"{safeName}.gif");

            if (overwriteExisting)
            {
                return path;
            }

            for (int suffix = 2; File.Exists(path); suffix++)
            {
                path = Path.Combine(folder, $"{safeName}_{suffix}.gif");
            }

            return path;
        }

        /// <summary>
        /// Builds the house-style HTML image block. Notes and labs sit one
        /// folder below Docs, so images in Docs are referenced with ../
        /// </summary>
        private string BuildImageBlock(string path)
        {
            string relative = GetProjectRelativePath(path);
            const string docsPrefix = "Docs/";

            string source = relative.StartsWith(docsPrefix, StringComparison.OrdinalIgnoreCase)
                ? "../" + relative.Substring(docsPrefix.Length)
                : relative;

            string alt = string.IsNullOrWhiteSpace(altText)
                ? "Describe the movement shown in this animation"
                : altText;

            int imageWidth = lastSavedWidth > 0 ? lastSavedWidth : width;

            return ImageBlock.Build(source, alt, imageWidth) + "\n";
        }

        /// <summary>
        /// Tells listeners, such as Lab Media Capture, that a GIF was saved.
        /// A listener's error is logged rather than reported as a failed save,
        /// because the file is already on disk.
        /// </summary>
        private static void RaiseSaved(string path, int savedWidth)
        {
            try
            {
                Saved?.Invoke(path, savedWidth);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Opens a folder picker. Folders inside the project are stored as
        /// project-relative paths so the setting still works if the project moves.
        /// </summary>
        private void BrowseForOutputFolder()
        {
            string startFolder = Path.Combine(GetProjectRootPath(), outputFolder.Trim());

            if (!Directory.Exists(startFolder))
            {
                startFolder = GetProjectRootPath();
            }

            string selected = EditorUtility.OpenFolderPanel("Choose GIF Output Folder", startFolder, string.Empty);

            if (string.IsNullOrEmpty(selected))
            {
                return;
            }

            string projectRoot = GetProjectRootPath().Replace('\\', '/');
            string normalised = selected.Replace('\\', '/');

            // "." keeps the field non-empty, which Record requires.
            outputFolder = string.Equals(normalised, projectRoot, StringComparison.OrdinalIgnoreCase)
                ? "."
                : GetProjectRelativePath(selected);

            // Clear keyboard focus so the text field shows the new value.
            GUI.FocusControl(null);
        }

        private void SetSize(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;
            GUI.FocusControl(null);
        }

        private void ReleaseCaptureTargets()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
                renderTexture = null;
            }

            if (readback != null)
            {
                DestroyImmediate(readback);
                readback = null;
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }

        private void ClearStatus()
        {
            SetStatus(string.Empty, MessageType.None);
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
            {
                return normalisedPath;
            }

            return normalisedPath.Substring(projectRoot.Length + 1);
        }

        /// <summary>
        /// Labels and tooltips for every control, kept together so the wording
        /// is easy to review.
        /// </summary>
        private static class Labels
        {
            public static readonly GUIContent Camera = new GUIContent(
                "Camera",
                "The camera to record. Leave empty to use the camera tagged MainCamera when recording starts. " +
                "Only what this camera renders is captured: Screen Space - Overlay UI and Editor overlays are not.");

            public static readonly GUIContent Width = new GUIContent(
                "Width",
                "Width of the GIF in pixels (16-1920). 640 suits a note column; larger sizes grow the file quickly.");

            public static readonly GUIContent Height = new GUIContent(
                "Height",
                "Height of the GIF in pixels (16-1080). Match the Game view's aspect ratio, or the image is stretched.");

            public static readonly GUIContent Size480 = new GUIContent(
                "480 x 270",
                "Small 16:9. Use for close-ups or when several GIFs share a page.");

            public static readonly GUIContent Size640 = new GUIContent(
                "640 x 360",
                "Medium 16:9. The default for notes and labs.");

            public static readonly GUIContent Size800 = new GUIContent(
                "800 x 450",
                "Large 16:9. Use when small details must be readable; expect about 1.5x the file size of 640 x 360.");

            public static readonly GUIContent Supersample = new GUIContent(
                "Supersample 2x",
                "Renders at double width and height, then averages each 2x2 block into one pixel. " +
                "Smooths jagged edges, at the cost of four times the rendering and readback work while recording.");

            public static readonly GUIContent FramesPerSecond = new GUIContent(
                "Frames Per Second",
                "How many frames are captured per second of game time. " +
                "10-15 is smooth enough for movement and keeps files small; 30 roughly doubles the size of 15.");

            public static readonly GUIContent MaxDuration = new GUIContent(
                "Max Duration (s)",
                "Recording stops automatically after this many seconds. " +
                "You can also stop early with the hotkey, Stop Recording, or by leaving Play Mode.");

            public static readonly GUIContent StartDelay = new GUIContent(
                "Start Delay (s)",
                "Countdown between starting a recording and the first captured frame, so you can get your hands on the controls. " +
                "The countdown shows over the Game view and in this window.");

            public static readonly GUIContent RecordOnPlay = new GUIContent(
                "Record On Play",
                "Starts recording, after the start delay, as soon as Play Mode begins. " +
                "Use for movement that runs without input, such as a NavMesh agent.");

            public static readonly GUIContent HotkeyEnabled = new GUIContent(
                "Use Hotkey",
                "Lets a key start and stop recording from the Game view, so your hands stay on the controls. " +
                "Pressing it during the countdown cancels the recording.");

            public static readonly GUIContent Hotkey = new GUIContent(
                "Hotkey",
                "The key that starts and stops recording. It only works while the Game view has focus, " +
                "because that is when the Input System receives keys. Choose a key the game does not use; " +
                "F9 is not bound in InputSystem_Actions.");

            public static readonly GUIContent ShowKeys = new GUIContent(
                "Show Keys In GIF",
                "Draws W, A, S, D, Shift and Space into every frame, lit while held, so readers can see " +
                "which inputs caused the movement. Arrow keys and a gamepad's left stick or d-pad light the WASD keys. " +
                "Unlike the overlay, this is part of the GIF.");

            public static readonly GUIContent KeysPosition = new GUIContent(
                "Position",
                "Where the key display sits in the GIF. Pick a corner away from the action.");

            public static readonly GUIContent ShowOverlay = new GUIContent(
                "Show Overlay",
                "Shows the countdown, a REC timer and the saved file name over the Game view. " +
                "The overlay is drawn by the Editor, not the camera, so it never appears in the GIF.");

            public static readonly GUIContent OverlayPosition = new GUIContent(
                "Position",
                "Where the overlay sits in the Game view. Pick a corner away from the action you are recording, " +
                "so it does not hide what you need to watch.");

            public static readonly GUIContent OverlayOpacity = new GUIContent(
                "Opacity",
                "How solid the overlay is, from 0.1 (faint) to 1 (solid). " +
                "Lower values let you see the scene through it; opacity has no effect on the GIF.");

            public static readonly GUIContent PreviewOverlay = new GUIContent(
                "Preview Overlay",
                "Shows a sample overlay in the Game view for 3 seconds, using the current position and opacity. " +
                "Works in or out of Play Mode.");

            public static readonly GUIContent ReviewBeforeSaving = new GUIContent(
                "Review Before Saving",
                "When ticked, stopping a recording opens the take here so you can scrub it, trim the start and end, " +
                "check the file size and then save. When cleared, the whole take is saved straight away.");

            public static readonly GUIContent SizeBudget = new GUIContent(
                "Size Budget (KB)",
                "The largest file you want in the repository. The review shows the GIF's size against this, " +
                "and offers fewer frames or half resolution when it is over. 2048 KB (2 MB) suits a note.");

            public static readonly GUIContent Dither = new GUIContent(
                "Ordered Dither",
                "A GIF holds at most 256 colours, so smooth gradients such as skies show bands. " +
                "Dithering mixes nearby colours in a fixed pattern to hide the bands, but makes the file larger.");

            public static readonly GUIContent Loop = new GUIContent(
                "Loop",
                "When ticked, the GIF repeats forever. When cleared, it plays once and stops on the last frame.");

            public static readonly GUIContent OutputFolder = new GUIContent(
                "Project Folder",
                "Where the GIF is saved, relative to the project root (the folder that contains Assets). " +
                "Saving to Docs/images lets notes and labs reference it as ../images/file.gif.");

            public static readonly GUIContent Browse = new GUIContent(
                "Browse",
                "Pick the output folder. Folders inside the project are stored as relative paths.");

            public static readonly GUIContent FileName = new GUIContent(
                "File Name",
                "Name of the GIF, without the extension. Spaces become underscores. " +
                "An existing file is kept and a number such as _2 is added, unless Overwrite Existing is ticked.");

            public static readonly GUIContent OverwriteExisting = new GUIContent(
                "Overwrite Existing",
                "Replace a GIF of the same name. Ticked by Lab Media Capture, because a lab's image path is fixed.");

            public static readonly GUIContent AltText = new GUIContent(
                "Alt Text",
                "Describes the animation for readers who cannot see it: what moves, where, and what happens. " +
                "Used by Copy HTML Image Block.");

            public static readonly GUIContent Record = new GUIContent(
                "Record",
                "Starts the countdown, then captures frames. Focus moves to the Game view so you can control the player.");

            public static readonly GUIContent Stop = new GUIContent(
                "Stop Recording",
                "Stops recording. With Review Before Saving ticked, the take opens here for trimming; otherwise it is saved.");

            public static readonly GUIContent Play = new GUIContent(
                "Play",
                "Plays the trimmed range on a loop, at the speed and frame step it will have in the GIF.");

            public static readonly GUIContent Pause = new GUIContent(
                "Pause",
                "Stops the preview on the current frame.");

            public static readonly GUIContent Trim = new GUIContent(
                "Trim",
                "Drag the ends to choose the first and last frames to keep. Frames outside the range are not saved.");

            public static readonly GUIContent SetIn = new GUIContent(
                "Set Start Here",
                "Makes the frame shown above the first frame of the GIF.");

            public static readonly GUIContent SetOut = new GUIContent(
                "Set End Here",
                "Makes the frame shown above the last frame of the GIF.");

            public static readonly GUIContent ResetTrim = new GUIContent(
                "Reset",
                "Keeps the whole take again.");

            public static readonly GUIContent FrameStep = new GUIContent(
                "Frames Kept",
                "Keeps every frame, or every 2nd or 3rd. Fewer frames make a smaller file and slightly choppier motion; " +
                "the GIF still plays at the original speed.");

            public static readonly GUIContent HalfResolution = new GUIContent(
                "Half Resolution",
                "Saves the GIF at half the width and height, which roughly quarters the file size. " +
                "Remember the copied image block uses the saved width.");

            public static readonly GUIContent EstimateSize = new GUIContent(
                "Estimate Size",
                "Encodes the GIF with the current trim and settings to find its exact size. " +
                "Saving afterwards reuses this result, so it costs no extra time.");

            public static readonly GUIContent EstimatedSize = new GUIContent(
                "GIF Size",
                "The exact size of the GIF with the current trim and settings.");

            public static readonly GUIContent FewerFrames = new GUIContent(
                "Keep Fewer Frames",
                "Keeps every 2nd frame (or every 3rd if already at every 2nd), then estimates the size again.");

            public static readonly GUIContent HalveResolution = new GUIContent(
                "Halve Resolution",
                "Turns on Half Resolution, then estimates the size again.");

            public static readonly GUIContent SaveTake = new GUIContent(
                "Save GIF",
                "Saves the trimmed take with the settings above, then closes the review.");

            public static readonly GUIContent DiscardTake = new GUIContent(
                "Discard",
                "Throws the take away without saving, so you can record again.");

            public static readonly GUIContent ShowInExplorer = new GUIContent(
                "Show in Explorer",
                "Opens the folder containing the last saved GIF.");

            public static readonly GUIContent CopyImageBlock = new GUIContent(
                "Copy HTML Image Block",
                "Copies a house-style centred <img> block for the last GIF to the clipboard, " +
                "with its path relative to the notes and labs folders, the alt text and the saved width.");
        }
    }
}
