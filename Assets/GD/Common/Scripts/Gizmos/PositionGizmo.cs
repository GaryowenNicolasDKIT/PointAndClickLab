using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GizmoTools
{
    /// <summary>
    /// Editor-only visualisation for empty GameObjects (spawn points, waypoints, anchors, etc.).
    /// Safe to leave on objects in builds: all drawing code is stripped outside the editor.
    /// </summary>
    [AddComponentMenu("GD/Gizmos/Position Gizmo")]
    public class PositionGizmo : MonoBehaviour
    {
        public enum Shape { None, Sphere, WireSphere, Cube, WireCube, Cross, Diamond, Disc, WireDisc, Icon }
        public enum Visibility { Always, SelectedOnly, UnselectedOnly }
        public enum SizeMode { World, ScreenConstant }
        public enum DiscAxis { X, Y, Z }
        public enum LabelContent { None, Name, Custom, Position, NameAndPosition, CustomAndPosition }

        [Header("Visibility")]
        [Tooltip("Master toggle for this gizmo. The global toggle under Tools > Position Gizmos overrides it.")]
        public bool show = true;
        [Tooltip("Always: draw regardless of selection.\nSelectedOnly: draw only when this object is selected.\nUnselectedOnly: hide while selected, e.g. to keep the transform handle clear.")]
        public Visibility visibility = Visibility.Always;
        [Tooltip("Suppress drawing while the editor is in Play mode.")]
        public bool hideInPlayMode = false;
        [Tooltip("Cull beyond this distance from the scene camera. 0 = unlimited.")]
        [Min(0f)] public float maxDrawDistance = 0f;
        [Tooltip("Fraction of maxDrawDistance over which the gizmo fades out. 0 = hard cut-off. Ignored when maxDrawDistance is 0.")]
        [Range(0f, 1f)] public float fadeBand = 0.2f;

        [Header("Shape")]
        [Tooltip("Primitive drawn at the position. Solid shapes and Disc are clickable in the Scene view; wire-only shapes are not.")]
        public Shape shape = Shape.WireSphere;
        [Tooltip("World: size is in world units.\nScreenConstant: size is a multiplier of handle size, so the gizmo keeps the same on-screen size at any zoom.")]
        public SizeMode sizeMode = SizeMode.World;
        [Tooltip("Overall extent (diameter / edge length). In ScreenConstant mode this is a multiplier of handle size.")]
        [Min(0.001f)] public float size = 0.25f;
        [Tooltip("Offset from the transform's position. Applied in local space when inheritRotation is on, otherwise in world space. Labels and the forward arrow follow it; hierarchy lines do not.")]
        public Vector3 offset = Vector3.zero;
        [Tooltip("Orient the shape (and offset) with the transform's rotation.")]
        public bool inheritRotation = true;
        [Tooltip("Scale the shape by the transform's lossy scale. Non-uniform scale will distort spheres and discs.")]
        public bool inheritScale = false;
        [Tooltip("Normal axis of the disc for Disc and WireDisc. Y gives a flat ground marker.")]
        public DiscAxis discAxis = DiscAxis.Y;
        [Tooltip("File name of an icon in Assets/Gizmos, e.g. 'spawn.png'. Used only by the Icon shape.")]
        public string iconName = "";
        [Tooltip("Scale the icon with camera distance. Off keeps it at a fixed pixel size.")]
        public bool iconAllowScaling = true;

        [Header("Colour")]
        [Tooltip("Base colour. Alpha is respected and combined with the distance fade.")]
        public Color color = new Color(1f, 0.8f, 0f, 1f);
        [Tooltip("Swap to selectedColor while this object is selected.")]
        public bool useSelectedColor = true;
        [Tooltip("Colour used while selected, if useSelectedColor is on.")]
        public Color selectedColor = new Color(0.2f, 0.9f, 1f, 1f);
        [Tooltip("Alpha multiplier for the fill of solid shapes (Sphere, Cube, Disc). Outlines use the full colour alpha.")]
        [Range(0f, 1f)] public float solidAlpha = 0.35f;
        [Tooltip("Draw a wire outline around solid shapes.")]
        public bool outlineSolids = true;

        [Header("Direction")]
        [Tooltip("Draw an arrow along transform.forward. Useful for spawn facing or patrol direction.")]
        public bool showForward = false;
        [Tooltip("Arrow length in world units.")]
        [Min(0f)] public float forwardLength = 1f;
        public Color forwardColor = new Color(0.3f, 0.5f, 1f, 1f);
        [Tooltip("Draw RGB = XYZ axis lines.")]
        public bool showAxes = false;
        [Tooltip("Axis line length in world units.")]
        [Min(0f)] public float axesLength = 0.5f;
        [Tooltip("Align the axes with the transform's rotation. Off draws world axes.")]
        public bool axesInLocalSpace = true;

        [Header("Hierarchy Lines")]
        [Tooltip("Draw a line from this transform to its parent.")]
        public bool lineToParent = false;
        [Tooltip("Draw lines to each direct child (not grandchildren). Handy for waypoint groups.")]
        public bool lineToChildren = false;
        public Color hierarchyLineColor = new Color(1f, 1f, 1f, 0.4f);

        [Header("Label")]
        [Tooltip("What to show as a Scene view text label.")]
        public LabelContent label = LabelContent.None;
        [Tooltip("Text used by the Custom and CustomAndPosition label modes.")]
        public string customLabel = "";
        [Tooltip("World-space offset of the label from the gizmo position.")]
        public Vector3 labelOffset = new Vector3(0f, 0.4f, 0f);
        public Color labelColor = Color.white;
        [Tooltip("Font size in points. Labels stay a constant screen size regardless of distance.")]
        [Min(1)] public int labelFontSize = 11;
        public bool labelBold = false;
        [Tooltip("Show localPosition instead of world position in position labels.")]
        public bool labelLocalPosition = false;
        [Tooltip("Decimal places shown in position labels.")]
        [Range(0, 5)] public int positionDecimals = 2;

#if UNITY_EDITOR
        private const string GlobalPrefKey = "GizmoTools.PositionGizmo.Enabled";
        private static GUIStyle labelStyle;

        public static bool GlobalEnabled
        {
            get => EditorPrefs.GetBool(GlobalPrefKey, true);
            set { EditorPrefs.SetBool(GlobalPrefKey, value); SceneView.RepaintAll(); }
        }

        [MenuItem("Tools/Position Gizmos/Enabled")]
        private static void ToggleGlobal() => GlobalEnabled = !GlobalEnabled;

        [MenuItem("Tools/Position Gizmos/Enabled", true)]
        private static bool ToggleGlobalValidate()
        {
            Menu.SetChecked("Tools/Position Gizmos/Enabled", GlobalEnabled);
            return true;
        }

        private void OnDrawGizmos()
        {
            if (!show || !GlobalEnabled) return;
            if (hideInPlayMode && Application.isPlaying) return;

            bool selected = Selection.Contains(gameObject);
            if (visibility == Visibility.SelectedOnly && !selected) return;
            if (visibility == Visibility.UnselectedOnly && selected) return;

            Vector3 worldPos = transform.position + (inheritRotation ? transform.rotation * offset : offset);

            float alphaScale = DistanceAlpha(worldPos);
            if (alphaScale <= 0f) return;

            Color baseCol = (selected && useSelectedColor) ? selectedColor : color;
            baseCol.a *= alphaScale;

            float s = sizeMode == SizeMode.ScreenConstant ? HandleUtility.GetHandleSize(worldPos) * size : size;

            Matrix4x4 prevGizmo = Gizmos.matrix;
            Matrix4x4 prevHandles = Handles.matrix;
            Color prevGizmoCol = Gizmos.color;
            Color prevHandlesCol = Handles.color;

            Quaternion rot = inheritRotation ? transform.rotation : Quaternion.identity;
            Vector3 scl = inheritScale ? transform.lossyScale : Vector3.one;
            Matrix4x4 m = Matrix4x4.TRS(worldPos, rot, scl);

            DrawShape(m, worldPos, s, baseCol);

            Gizmos.matrix = Matrix4x4.identity;
            Handles.matrix = Matrix4x4.identity;

            if (showAxes) DrawAxes(worldPos, alphaScale);
            if (showForward) DrawForward(worldPos, alphaScale);
            if (lineToParent || lineToChildren) DrawHierarchyLines(alphaScale);
            if (label != LabelContent.None) DrawLabel(worldPos, alphaScale);

            Gizmos.matrix = prevGizmo;
            Handles.matrix = prevHandles;
            Gizmos.color = prevGizmoCol;
            Handles.color = prevHandlesCol;
        }

        private float DistanceAlpha(Vector3 worldPos)
        {
            if (maxDrawDistance <= 0f) return 1f;
            Camera cam = Camera.current;
            if (cam == null) return 1f;

            float d = Vector3.Distance(cam.transform.position, worldPos);
            if (d >= maxDrawDistance) return 0f;
            if (fadeBand <= 0f) return 1f;

            float fadeStart = maxDrawDistance * (1f - fadeBand);
            return d <= fadeStart ? 1f : 1f - Mathf.InverseLerp(fadeStart, maxDrawDistance, d);
        }

        private void DrawShape(Matrix4x4 m, Vector3 worldPos, float s, Color col)
        {
            Color solid = col;
            solid.a *= solidAlpha;
            float r = s * 0.5f;

            Gizmos.matrix = m;
            Handles.matrix = m;

            switch (shape)
            {
                case Shape.Sphere:
                    Gizmos.color = solid;
                    Gizmos.DrawSphere(Vector3.zero, r);
                    if (outlineSolids) { Gizmos.color = col; Gizmos.DrawWireSphere(Vector3.zero, r); }
                    break;

                case Shape.WireSphere:
                    Gizmos.color = col;
                    Gizmos.DrawWireSphere(Vector3.zero, r);
                    break;

                case Shape.Cube:
                    Gizmos.color = solid;
                    Gizmos.DrawCube(Vector3.zero, Vector3.one * s);
                    if (outlineSolids) { Gizmos.color = col; Gizmos.DrawWireCube(Vector3.zero, Vector3.one * s); }
                    break;

                case Shape.WireCube:
                    Gizmos.color = col;
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one * s);
                    break;

                case Shape.Cross:
                    Gizmos.color = col;
                    Gizmos.DrawLine(Vector3.left * r, Vector3.right * r);
                    Gizmos.DrawLine(Vector3.down * r, Vector3.up * r);
                    Gizmos.DrawLine(Vector3.back * r, Vector3.forward * r);
                    break;

                case Shape.Diamond:
                    Gizmos.color = col;
                    DrawOctahedron(r);
                    break;

                case Shape.Disc:
                    Handles.color = solid;
                    Handles.DrawSolidDisc(Vector3.zero, DiscNormal(), r);
                    if (outlineSolids) { Handles.color = col; Handles.DrawWireDisc(Vector3.zero, DiscNormal(), r); }
                    // Invisible-ish sphere keeps the object pickable in the scene view.
                    Gizmos.color = new Color(0f, 0f, 0f, 0f);
                    Gizmos.DrawSphere(Vector3.zero, r);
                    break;

                case Shape.WireDisc:
                    Handles.color = col;
                    Handles.DrawWireDisc(Vector3.zero, DiscNormal(), r);
                    break;

                case Shape.Icon:
                    Gizmos.matrix = Matrix4x4.identity;
                    if (!string.IsNullOrEmpty(iconName))
                        Gizmos.DrawIcon(worldPos, iconName, iconAllowScaling, col);
                    break;
            }
        }

        private Vector3 DiscNormal()
        {
            switch (discAxis)
            {
                case DiscAxis.X: return Vector3.right;
                case DiscAxis.Z: return Vector3.forward;
                default: return Vector3.up;
            }
        }

        private static void DrawOctahedron(float r)
        {
            Vector3 up = Vector3.up * r, down = Vector3.down * r;
            Vector3[] ring = { Vector3.right * r, Vector3.forward * r, Vector3.left * r, Vector3.back * r };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = ring[i], b = ring[(i + 1) % 4];
                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(a, up);
                Gizmos.DrawLine(a, down);
            }
        }

        private void DrawAxes(Vector3 origin, float alphaScale)
        {
            Quaternion q = axesInLocalSpace ? transform.rotation : Quaternion.identity;
            DrawLine(origin, origin + q * Vector3.right * axesLength, WithAlpha(Color.red, alphaScale));
            DrawLine(origin, origin + q * Vector3.up * axesLength, WithAlpha(Color.green, alphaScale));
            DrawLine(origin, origin + q * Vector3.forward * axesLength, WithAlpha(Color.blue, alphaScale));
        }

        private void DrawForward(Vector3 origin, float alphaScale)
        {
            if (forwardLength <= 0f) return;
            Color c = WithAlpha(forwardColor, alphaScale);
            Vector3 dir = transform.forward;
            Vector3 end = origin + dir * forwardLength;

            DrawLine(origin, end, c);

            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                float coneSize = Mathf.Min(forwardLength * 0.2f, HandleUtility.GetHandleSize(end) * 0.15f);
                Handles.color = c;
                Handles.ConeHandleCap(0, end - dir * coneSize * 0.5f, Quaternion.LookRotation(dir), coneSize, EventType.Repaint);
            }
        }

        private void DrawHierarchyLines(float alphaScale)
        {
            Color c = WithAlpha(hierarchyLineColor, alphaScale);
            Vector3 p = transform.position;

            if (lineToParent && transform.parent != null)
                DrawLine(p, transform.parent.position, c);

            if (lineToChildren)
                foreach (Transform child in transform)
                    DrawLine(p, child.position, c);
        }

        private void DrawLabel(Vector3 worldPos, float alphaScale)
        {
            string text = BuildLabelText();
            if (string.IsNullOrEmpty(text)) return;

            if (labelStyle == null) labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = WithAlpha(labelColor, alphaScale);
            labelStyle.fontSize = labelFontSize;
            labelStyle.fontStyle = labelBold ? FontStyle.Bold : FontStyle.Normal;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            Handles.Label(worldPos + labelOffset, text, labelStyle);
        }

        private string BuildLabelText()
        {
            string pos = FormatPosition();
            switch (label)
            {
                case LabelContent.Name: return name;
                case LabelContent.Custom: return customLabel;
                case LabelContent.Position: return pos;
                case LabelContent.NameAndPosition: return name + "\n" + pos;
                case LabelContent.CustomAndPosition: return customLabel + "\n" + pos;
                default: return null;
            }
        }

        private string FormatPosition()
        {
            Vector3 p = labelLocalPosition ? transform.localPosition : transform.position;
            string f = "F" + positionDecimals;
            return $"({p.x.ToString(f)}, {p.y.ToString(f)}, {p.z.ToString(f)})";
        }

        private static void DrawLine(Vector3 a, Vector3 b, Color c)
        {
            Gizmos.color = c;
            Gizmos.DrawLine(a, b);
        }

        private static Color WithAlpha(Color c, float scale)
        {
            c.a *= scale;
            return c;
        }
#endif
    }
}