using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GD
{
    /// <summary>
    /// Where the recording overlay sits in the Game view.
    /// </summary>
    public enum OverlayPosition
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

    /// <summary>
    /// A status label added to the Game view's UI Toolkit root.
    /// It is Editor UI drawn over the Game view, not part of any camera's
    /// render, so it never appears in captured frames. It ignores the
    /// mouse, so clicks still reach the game underneath.
    /// </summary>
    public sealed class GameViewRecordingOverlay
    {
        private const string ElementName = "GifCaptureRecordingOverlay";

        // Keeps the overlay clear of the Game view's toolbar.
        private const float ToolbarClearance = 28f;
        private const float EdgeMargin = 10f;

        private readonly VisualElement container;
        private readonly Label label;
        private IVisualElementScheduledItem pendingHide;

        public GameViewRecordingOverlay()
        {
            // A full-size, click-through container; flexbox alignment places
            // the label in one of nine positions.
            container = new VisualElement { name = ElementName, pickingMode = PickingMode.Ignore };
            container.style.position = Position.Absolute;
            container.style.left = 0f;
            container.style.right = 0f;
            container.style.top = 0f;
            container.style.bottom = 0f;
            container.style.paddingTop = ToolbarClearance;
            container.style.paddingBottom = EdgeMargin;
            container.style.paddingLeft = EdgeMargin;
            container.style.paddingRight = EdgeMargin;
            container.style.flexDirection = FlexDirection.Column;

            label = new Label { pickingMode = PickingMode.Ignore };
            label.style.color = Color.white;
            label.style.fontSize = 18;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.paddingTop = 6f;
            label.style.paddingBottom = 6f;
            label.style.paddingLeft = 12f;
            label.style.paddingRight = 12f;
            label.style.borderTopLeftRadius = 4f;
            label.style.borderTopRightRadius = 4f;
            label.style.borderBottomLeftRadius = 4f;
            label.style.borderBottomRightRadius = 4f;

            container.Add(label);
        }

        /// <summary>
        /// Finds an open Game view without changing focus. The Game view's
        /// class is internal to the Editor, so it is identified by name.
        /// </summary>
        public static EditorWindow FindGameView()
        {
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType().Name == "GameView")
                {
                    return window;
                }
            }

            return null;
        }

        /// <summary>
        /// Removes overlays left behind in any Game view, for example by a
        /// script reload while a recording was running.
        /// </summary>
        public static void RemoveStrayOverlays()
        {
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType().Name == "GameView")
                {
                    window.rootVisualElement.Q(ElementName)?.RemoveFromHierarchy();
                }
            }
        }

        /// <summary>
        /// Shows the overlay in the Game view, or updates it if already shown.
        /// </summary>
        public void Show(EditorWindow gameView, string text, Color background, OverlayPosition position, float opacity)
        {
            if (gameView == null)
            {
                return;
            }

            pendingHide?.Pause();
            pendingHide = null;

            if (container.parent != gameView.rootVisualElement)
            {
                container.RemoveFromHierarchy();
                gameView.rootVisualElement.Add(container);
            }

            if (label.text != text)
            {
                label.text = text;
            }

            label.style.backgroundColor = background;
            container.style.opacity = Mathf.Clamp01(opacity);
            ApplyPosition(position);
        }

        /// <summary>
        /// Shows a message, then removes the overlay after a delay.
        /// </summary>
        public void ShowThenHide(EditorWindow gameView, string text, Color background,
            OverlayPosition position, float opacity, long hideAfterMilliseconds)
        {
            Show(gameView, text, background, position, opacity);

            if (container.parent != null)
            {
                pendingHide = container.schedule.Execute(Hide).StartingIn(hideAfterMilliseconds);
            }
        }

        /// <summary>
        /// True while the overlay is attached to a Game view.
        /// </summary>
        public bool IsShown => container.parent != null;

        /// <summary>
        /// Applies a new position and opacity to the overlay already shown.
        /// If it is due to hide, the countdown restarts, so it stays visible
        /// while the settings are being adjusted.
        /// </summary>
        public void UpdateAppearance(OverlayPosition position, float opacity, long hideAfterMilliseconds)
        {
            if (!IsShown)
            {
                return;
            }

            container.style.opacity = Mathf.Clamp01(opacity);
            ApplyPosition(position);

            if (pendingHide != null)
            {
                pendingHide.Pause();
                pendingHide = container.schedule.Execute(Hide).StartingIn(hideAfterMilliseconds);
            }
        }

        /// <summary>
        /// Removes the overlay from the Game view.
        /// </summary>
        public void Hide()
        {
            pendingHide?.Pause();
            pendingHide = null;
            container.RemoveFromHierarchy();
        }

        private void ApplyPosition(OverlayPosition position)
        {
            int column = (int)position % 3;
            int row = (int)position / 3;

            // In a column layout, alignItems is horizontal and justifyContent vertical.
            container.style.alignItems = column == 0 ? Align.FlexStart : column == 1 ? Align.Center : Align.FlexEnd;
            container.style.justifyContent = row == 0 ? Justify.FlexStart : row == 1 ? Justify.Center : Justify.FlexEnd;
        }
    }
}
