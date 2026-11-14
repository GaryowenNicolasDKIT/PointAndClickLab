using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GD
{
    /// <summary>
    /// Displays mesh and renderer information for the current Unity selection.
    /// Useful for inspecting geometry complexity, materials, shadow settings,
    /// static flags, and LOD membership.
    /// </summary>
    public class MeshRendererInspector : EditorWindow
    {
        private sealed class RendererInfo
        {
            public GameObject GameObject;
            public Renderer Renderer;
            public Mesh Mesh;
            public int VertexCount;
            public long TriangleCount;
            public int SubMeshCount;
            public int MaterialCount;
            public bool IsStatic;
            public StaticEditorFlags StaticFlags;
            public LODGroup LodGroup;
            public int LodLevel = -1;
        }

        private readonly List<RendererInfo> rendererInfos = new();

        private Vector2 scrollPosition;
        private bool includeChildren = true;
        private bool showPerObjectDetails = true;
        private string statusMessage = string.Empty;

        /// <summary>
        /// Opens the Mesh / Renderer Inspector window.
        /// </summary>
        [MenuItem("Tools/GD/Analyse/Mesh and Renderer Inspector", priority = 202)]
        private static void ShowWindow()
        {
            MeshRendererInspector window =
                GetWindow<MeshRendererInspector>();

            window.titleContent =
                new GUIContent("Mesh / Renderer Inspector");

            window.minSize =
                new Vector2(480f, 420f);

            window.Show();
        }

        /// <summary>
        /// Subscribes to selection changes and performs an initial refresh.
        /// </summary>
        private void OnEnable()
        {
            Selection.selectionChanged += Refresh;
            Refresh();
        }

        /// <summary>
        /// Unsubscribes from selection changes.
        /// </summary>
        private void OnDisable()
        {
            Selection.selectionChanged -= Refresh;
        }

        /// <summary>
        /// Draws the inspector window.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(6f);

            DrawToolbar();

            GUILayout.Space(6f);

            DrawSummary();

            GUILayout.Space(8f);

            DrawDetails();
        }

        /// <summary>
        /// Draws the window heading and guidance.
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "Mesh / Renderer Inspector",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Select one or more GameObjects to inspect mesh and renderer information. " +
                "Use this tool to identify geometry, material, shadow, static, and LOD costs.",
                MessageType.Info);
        }

        /// <summary>
        /// Draws the toolbar controls.
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            bool newIncludeChildren =
                GUILayout.Toggle(
                    includeChildren,
                    "Include Children",
                    EditorStyles.toolbarButton);

            if (newIncludeChildren != includeChildren)
            {
                includeChildren = newIncludeChildren;
                Refresh();
            }

            showPerObjectDetails =
                GUILayout.Toggle(
                    showPerObjectDetails,
                    "Details",
                    EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                    "Refresh",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(60f)))
            {
                Refresh();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws aggregate information for the current selection.
        /// </summary>
        private void DrawSummary()
        {
            EditorGUILayout.LabelField(
                "Selection Summary",
                EditorStyles.boldLabel);

            if (rendererInfos.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(statusMessage)
                        ? "No supported MeshRenderer or SkinnedMeshRenderer components were found."
                        : statusMessage,
                    MessageType.None);

                return;
            }

            long totalVertices =
                rendererInfos.Sum(info => (long)info.VertexCount);

            long totalTriangles =
                rendererInfos.Sum(info => info.TriangleCount);

            int totalMaterials =
                rendererInfos.Sum(info => info.MaterialCount);

            int uniqueMeshes =
                rendererInfos
                    .Where(info => info.Mesh != null)
                    .Select(info => info.Mesh)
                    .Distinct()
                    .Count();

            int uniqueMaterials =
                rendererInfos
                    .SelectMany(info => info.Renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Distinct()
                    .Count();

            int shadowCasters =
                rendererInfos.Count(
                    info =>
                        info.Renderer.shadowCastingMode !=
                        ShadowCastingMode.Off);

            int lodRenderers =
                rendererInfos.Count(
                    info => info.LodGroup != null);

            DrawMetric("Renderers", rendererInfos.Count.ToString());
            DrawMetric("Unique Meshes", uniqueMeshes.ToString());
            DrawMetric("Vertices", totalVertices.ToString("N0"));
            DrawMetric("Triangles", totalTriangles.ToString("N0"));
            DrawMetric("Material Slots", totalMaterials.ToString());
            DrawMetric("Unique Materials", uniqueMaterials.ToString());
            DrawMetric("Shadow Casters", shadowCasters.ToString());
            DrawMetric("In LOD Groups", lodRenderers.ToString());
        }

        /// <summary>
        /// Draws per-renderer details.
        /// </summary>
        private void DrawDetails()
        {
            if (!showPerObjectDetails ||
                rendererInfos.Count == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "Renderer Details",
                EditorStyles.boldLabel);

            scrollPosition =
                EditorGUILayout.BeginScrollView(
                    scrollPosition);

            foreach (RendererInfo info in rendererInfos)
            {
                DrawRendererInfo(info);
                GUILayout.Space(6f);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws information for one renderer.
        /// </summary>
        private static void DrawRendererInfo(
            RendererInfo info)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox);

            GUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(
                info.GameObject,
                typeof(GameObject),
                true);

            if (GUILayout.Button(
                    "Ping",
                    GUILayout.Width(42f)))
            {
                Selection.activeGameObject =
                    info.GameObject;

                EditorGUIUtility.PingObject(
                    info.GameObject);
            }

            GUILayout.EndHorizontal();

            DrawMetric(
                "Renderer",
                ObjectNames.NicifyVariableName(
                    info.Renderer.GetType().Name));

            DrawMetric(
                "Mesh",
                info.Mesh != null
                    ? info.Mesh.name
                    : "None");

            DrawMetric(
                "Vertices",
                info.VertexCount.ToString("N0"));

            DrawMetric(
                "Triangles",
                info.TriangleCount.ToString("N0"));

            DrawMetric(
                "Submeshes",
                info.SubMeshCount.ToString());

            DrawMetric(
                "Material Slots",
                info.MaterialCount.ToString());

            DrawMetric(
                "Shadows",
                GetShadowDescription(
                    info.Renderer));

            DrawMetric(
                "Static",
                info.IsStatic
                    ? info.StaticFlags.ToString()
                    : "No");

            DrawMetric(
                "LOD",
                GetLodDescription(info));

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a simple label/value metric row.
        /// </summary>
        private static void DrawMetric(
            string label,
            string value)
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                label,
                GUILayout.Width(120f));

            EditorGUILayout.LabelField(
                value);

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Refreshes the inspected renderer information.
        /// </summary>
        private void Refresh()
        {
            rendererInfos.Clear();
            statusMessage = string.Empty;

            GameObject[] selectedObjects =
                Selection.gameObjects;

            if (selectedObjects == null ||
                selectedObjects.Length == 0)
            {
                statusMessage =
                    "Select one or more GameObjects in the Hierarchy.";
                Repaint();
                return;
            }

            HashSet<Renderer> processedRenderers =
                new HashSet<Renderer>();

            foreach (GameObject selectedObject in selectedObjects)
            {
                IEnumerable<Renderer> renderers =
                    includeChildren
                        ? selectedObject.GetComponentsInChildren<Renderer>(true)
                        : selectedObject.GetComponents<Renderer>();

                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null ||
                        processedRenderers.Contains(renderer))
                    {
                        continue;
                    }

                    Mesh mesh =
                        GetMesh(renderer);

                    if (mesh == null)
                        continue;

                    processedRenderers.Add(renderer);

                    rendererInfos.Add(
                        CreateRendererInfo(
                            renderer,
                            mesh));
                }
            }

            rendererInfos.Sort(
                (left, right) =>
                {
                    string leftPath =
                        GetHierarchyPath(
                            left.GameObject.transform);

                    string rightPath =
                        GetHierarchyPath(
                            right.GameObject.transform);

                    return string.Compare(
                        leftPath,
                        rightPath,
                        StringComparison.OrdinalIgnoreCase);
                });

            Repaint();
        }

        /// <summary>
        /// Creates renderer information for a supported renderer.
        /// </summary>
        private static RendererInfo CreateRendererInfo(
            Renderer renderer,
            Mesh mesh)
        {
            StaticEditorFlags staticFlags =
                GameObjectUtility.GetStaticEditorFlags(
                    renderer.gameObject);

            LODGroup lodGroup =
                renderer.GetComponentInParent<LODGroup>();

            return new RendererInfo
            {
                GameObject = renderer.gameObject,
                Renderer = renderer,
                Mesh = mesh,
                VertexCount = mesh.vertexCount,
                TriangleCount = CountTriangles(mesh),
                SubMeshCount = mesh.subMeshCount,
                MaterialCount = renderer.sharedMaterials.Length,
                IsStatic = staticFlags != 0,
                StaticFlags = staticFlags,
                LodGroup = lodGroup,
                LodLevel = FindLodLevel(
                    lodGroup,
                    renderer)
            };
        }

        /// <summary>
        /// Returns the Mesh used by a supported Renderer.
        /// </summary>
        private static Mesh GetMesh(
            Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            if (renderer is MeshRenderer)
            {
                MeshFilter meshFilter =
                    renderer.GetComponent<MeshFilter>();

                return meshFilter != null
                    ? meshFilter.sharedMesh
                    : null;
            }

            return null;
        }

        /// <summary>
        /// Counts triangle primitives in triangle-topology submeshes.
        /// </summary>
        private static long CountTriangles(
            Mesh mesh)
        {
            if (mesh == null)
                return 0;

            long triangleCount = 0;

            for (int i = 0;
                 i < mesh.subMeshCount;
                 i++)
            {
                if (mesh.GetTopology(i) !=
                    MeshTopology.Triangles)
                {
                    continue;
                }

                triangleCount +=
                    (long)mesh.GetIndexCount(i) / 3L;
            }

            return triangleCount;
        }

        /// <summary>
        /// Finds the LOD level containing the supplied renderer.
        /// </summary>
        private static int FindLodLevel(
            LODGroup lodGroup,
            Renderer renderer)
        {
            if (lodGroup == null ||
                renderer == null)
            {
                return -1;
            }

            LOD[] lods =
                lodGroup.GetLODs();

            for (int i = 0;
                 i < lods.Length;
                 i++)
            {
                Renderer[] lodRenderers =
                    lods[i].renderers;

                for (int j = 0;
                     j < lodRenderers.Length;
                     j++)
                {
                    if (lodRenderers[j] ==
                        renderer)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns a compact description of shadow settings.
        /// </summary>
        private static string GetShadowDescription(
            Renderer renderer)
        {
            string casting =
                renderer.shadowCastingMode.ToString();

            string receiving =
                renderer.receiveShadows
                    ? "Receive"
                    : "No Receive";

            return $"{casting}, {receiving}";
        }

        /// <summary>
        /// Returns a compact description of LOD membership.
        /// </summary>
        private static string GetLodDescription(
            RendererInfo info)
        {
            if (info.LodGroup == null)
            {
                return "Not in LOD Group";
            }

            if (info.LodLevel < 0)
            {
                return $"{info.LodGroup.name} (not assigned)";
            }

            return $"{info.LodGroup.name} / LOD {info.LodLevel}";
        }

        /// <summary>
        /// Builds a readable hierarchy path for sorting.
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
    }
}
