using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Displays serialized object-reference dependencies for components
    /// on the currently selected GameObject.
    /// </summary>
    public class ComponentDependencyViewer : EditorWindow
    {
        private sealed class DependencyEntry
        {
            public Component SourceComponent;
            public string PropertyPath;
            public string DisplayName;
            public UnityEngine.Object Target;
        }

        private readonly List<DependencyEntry> dependencies = new();

        private Vector2 scrollPosition;
        private bool includeChildren;
        private bool includeSelfReferences;
        private string searchText = string.Empty;

        /// <summary>
        /// Opens the Component Dependency Viewer.
        /// </summary>
        [MenuItem("Tools/GD/Analyse/Component Dependency Viewer", priority = 200)]
        private static void ShowWindow()
        {
            ComponentDependencyViewer window =
                GetWindow<ComponentDependencyViewer>();

            window.titleContent =
                new GUIContent("Component Dependencies");

            window.minSize =
                new Vector2(520f, 420f);

            window.Show();
        }

        /// <summary>
        /// Subscribes to selection changes.
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
        /// Draws the editor window.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(6f);

            DrawToolbar();

            GUILayout.Space(6f);

            DrawSummary();

            GUILayout.Space(6f);

            DrawDependencies();
        }

        /// <summary>
        /// Draws the heading and usage guidance.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "Component Dependency Viewer",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Select a GameObject to inspect serialized references used by its components. " +
                "This reveals explicit Inspector-assigned dependencies such as GameObjects, " +
                "Components, ScriptableObjects, Materials, AudioClips, and other Unity assets.",
                MessageType.Info);
        }

        /// <summary>
        /// Draws search and display options.
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            string newSearchText =
                GUILayout.TextField(
                    searchText,
                    GUI.skin.FindStyle("ToolbarSearchTextField"),
                    GUILayout.ExpandWidth(true));

            if (newSearchText != searchText)
            {
                searchText = newSearchText;
                Repaint();
            }

            if (!string.IsNullOrEmpty(searchText) &&
                GUILayout.Button(
                    GUIContent.none,
                    GUI.skin.FindStyle("ToolbarSearchCancelButton")))
            {
                searchText = string.Empty;
                GUI.FocusControl(null);
                Repaint();
            }

            bool newIncludeChildren =
                GUILayout.Toggle(
                    includeChildren,
                    "Children",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(65f));

            if (newIncludeChildren != includeChildren)
            {
                includeChildren = newIncludeChildren;
                Refresh();
            }

            bool newIncludeSelfReferences =
                GUILayout.Toggle(
                    includeSelfReferences,
                    "Self Refs",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(65f));

            if (newIncludeSelfReferences != includeSelfReferences)
            {
                includeSelfReferences = newIncludeSelfReferences;
                Refresh();
            }

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
        /// Draws a summary for the current selection.
        /// </summary>
        private void DrawSummary()
        {
            GameObject selectedObject =
                Selection.activeGameObject;

            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a GameObject in the Hierarchy.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.LabelField(
                "Selection",
                EditorStyles.boldLabel);

            EditorGUILayout.ObjectField(
                selectedObject,
                typeof(GameObject),
                true);

            int uniqueTargets =
                dependencies
                    .Where(entry => entry.Target != null)
                    .Select(entry => entry.Target)
                    .Distinct()
                    .Count();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "Serialized References",
                GUILayout.Width(140f));
            EditorGUILayout.LabelField(
                dependencies.Count.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "Unique Targets",
                GUILayout.Width(140f));
            EditorGUILayout.LabelField(
                uniqueTargets.ToString());
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws dependency entries grouped by source component.
        /// </summary>
        private void DrawDependencies()
        {
            if (Selection.activeGameObject == null)
                return;

            List<DependencyEntry> filtered =
                GetFilteredDependencies();

            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrWhiteSpace(searchText)
                        ? "No serialized object-reference dependencies were found."
                        : "No dependencies match the current search.",
                    MessageType.None);
                return;
            }

            scrollPosition =
                EditorGUILayout.BeginScrollView(scrollPosition);

            IEnumerable<IGrouping<Component, DependencyEntry>> groups =
                filtered
                    .GroupBy(entry => entry.SourceComponent)
                    .OrderBy(group => group.Key.GetType().Name);

            foreach (IGrouping<Component, DependencyEntry> group in groups)
            {
                DrawComponentGroup(group.Key, group);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws dependencies for a single component.
        /// </summary>
        private static void DrawComponentGroup(
            Component component,
            IEnumerable<DependencyEntry> entries)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox);

            EditorGUILayout.ObjectField(
                component,
                component.GetType(),
                true);

            foreach (DependencyEntry entry in entries)
            {
                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(
                    entry.DisplayName,
                    GUILayout.Width(170f));

                EditorGUILayout.ObjectField(
                    entry.Target,
                    typeof(UnityEngine.Object),
                    true);

                if (entry.Target != null &&
                    GUILayout.Button(
                        "Ping",
                        GUILayout.Width(42f)))
                {
                    Selection.activeObject = entry.Target;
                    EditorGUIUtility.PingObject(entry.Target);
                }

                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField(
                    entry.PropertyPath,
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        /// <summary>
        /// Refreshes serialized dependency information.
        /// </summary>
        private void Refresh()
        {
            dependencies.Clear();

            GameObject selectedObject =
                Selection.activeGameObject;

            if (selectedObject == null)
            {
                Repaint();
                return;
            }

            Component[] components =
                includeChildren
                    ? selectedObject.GetComponentsInChildren<Component>(true)
                    : selectedObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                CollectDependencies(component);
            }

            Repaint();
        }

        /// <summary>
        /// Collects serialized object-reference properties from a component.
        /// </summary>
        private void CollectDependencies(
            Component component)
        {
            SerializedObject serializedObject =
                new SerializedObject(component);

            SerializedProperty property =
                serializedObject.GetIterator();

            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyType !=
                    SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                UnityEngine.Object target =
                    property.objectReferenceValue;

                if (target == null)
                    continue;

                if (!includeSelfReferences &&
                    IsSelfReference(component, target))
                {
                    continue;
                }

                dependencies.Add(
                    new DependencyEntry
                    {
                        SourceComponent = component,
                        PropertyPath = property.propertyPath,
                        DisplayName = property.displayName,
                        Target = target
                    });
            }
        }

        /// <summary>
        /// Determines whether a serialized reference points back to
        /// the source GameObject or one of its own components.
        /// </summary>
        private static bool IsSelfReference(
            Component source,
            UnityEngine.Object target)
        {
            if (target == source.gameObject)
                return true;

            if (target is Component targetComponent &&
                targetComponent.gameObject == source.gameObject)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Filters dependencies by component, property, target, or path text.
        /// </summary>
        private List<DependencyEntry> GetFilteredDependencies()
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new List<DependencyEntry>(dependencies);
            }

            string query =
                searchText.Trim();

            return dependencies
                .Where(entry =>
                    entry.SourceComponent.GetType().Name.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase) ||
                    entry.DisplayName.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase) ||
                    entry.PropertyPath.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase) ||
                    (entry.Target != null &&
                     entry.Target.name.Contains(
                         query,
                         StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }
}

