using System;
using System.Collections.Generic;
using System.Linq;
using GD.Flow;
using GD.States;
using UnityEditor;
using UnityEngine;

namespace GD
{
    /// <summary>
    /// Shows, during Play, each FlowBehaviour's current step and each StateMachine's current
    /// state, transitions and recent changes.
    /// </summary>
    /// <remarks>
    /// Transition conditions are evaluated by this window so that it can show their answers.
    /// That is safe because a condition must change nothing when it is read.
    /// </remarks>
    public class StateFlowViewer : EditorWindow
    {
        private const float RepaintInterval = 0.1f;
        private const int HistoryLength = 8;

        // This many changes of state within one second is reported as flicker.
        private const int FlickerChangesPerSecond = 4;

        private static readonly Color ActiveColour = new(0.35f, 0.85f, 0.45f);
        private static readonly Color WarningColour = new(1f, 0.7f, 0.25f);
        private static readonly Color FalseColour = new(0.65f, 0.65f, 0.65f);

        private readonly HashSet<StateMachine> expandedHistories = new();

        private Vector2 scrollPosition;
        private string filter = string.Empty;
        private bool selectionOnly;
        private double lastRepaintTime;

        /// <summary>
        /// Opens the State and Flow Viewer.
        /// </summary>
        [MenuItem("Tools/GD/Analyse/State and Flow Viewer", priority = 203)]
        private static void ShowWindow()
        {
            StateFlowViewer window = GetWindow<StateFlowViewer>();
            window.titleContent = new GUIContent("State and Flow Viewer");
            window.minSize = new Vector2(420f, 320f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += RepaintDuringPlay;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintDuringPlay;
        }

        private void RepaintDuringPlay()
        {
            if (!EditorApplication.isPlaying)
                return;

            if (EditorApplication.timeSinceStartup - lastRepaintTime < RepaintInterval)
                return;

            lastRepaintTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to see flows and state machines.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawFlows();
            GUILayout.Space(8f);
            DrawStateMachines();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Filter", GUILayout.Width(36f));
            filter = EditorGUILayout.TextField(filter, EditorStyles.toolbarTextField);
            selectionOnly = GUILayout.Toggle(selectionOnly, "Selection only", EditorStyles.toolbarButton, GUILayout.Width(100f));
            EditorGUILayout.EndHorizontal();
        }

        #region Flows

        private void DrawFlows()
        {
            GUILayout.Label("Flows", EditorStyles.boldLabel);

            FlowBehaviour[] flows = FindObjectsByType<FlowBehaviour>(FindObjectsInactive.Include)
                .Where(flow => IsShown(flow, flow.GetType().Name))
                .OrderBy(flow => flow.name)
                .ToArray();

            if (flows.Length == 0)
                EditorGUILayout.LabelField("None found.", EditorStyles.miniLabel);

            foreach (FlowBehaviour flow in flows)
                DrawFlow(flow);
        }

        private static void DrawFlow(FlowBehaviour flow)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawOwnerHeader(flow, flow.GetType().Name);

            FlowSequence sequence = flow.Sequence;

            if (sequence == null)
            {
                EditorGUILayout.LabelField("Not built yet.");
                EditorGUILayout.EndVertical();
                return;
            }

            DrawColouredLabel(DescribeStatus(flow, sequence), flow.IsRunning ? ActiveColour : GUI.contentColor);

            for (int i = 0; i < sequence.Steps.Count; i++)
                DrawStep(flow, sequence, i);

            EditorGUILayout.EndVertical();
        }

        private static string DescribeStatus(FlowBehaviour flow, FlowSequence sequence)
        {
            if (flow.IsRunning && sequence.CurrentStepIndex >= 0)
            {
                float seconds = Time.unscaledTime - sequence.CurrentStepStartTime;
                return $"Running: step {sequence.CurrentStepIndex + 1} of {sequence.Steps.Count}, for {seconds:F1} s";
            }

            if (sequence.CurrentStepIndex >= 0)
                return $"Stopped at step {sequence.CurrentStepIndex + 1} of {sequence.Steps.Count}";

            if (!flow.isActiveAndEnabled)
                return "Inactive";

            return flow.HasPlayed ? "Finished" : "Waiting to be played";
        }

        private static void DrawStep(FlowBehaviour flow, FlowSequence sequence, int index)
        {
            bool isCurrent = index == sequence.CurrentStepIndex;
            string text = $"{(isCurrent ? ">" : " ")} {index + 1}. {sequence.Steps[index]}";

            if (!isCurrent)
            {
                EditorGUILayout.LabelField(text);
                return;
            }

            // A stopped flow never reaches its later steps, so that is worth a warning colour.
            DrawColouredLabel(text, flow.IsRunning ? ActiveColour : WarningColour, EditorStyles.boldLabel);
        }

        #endregion Flows

        #region State Machines

        private void DrawStateMachines()
        {
            GUILayout.Label("State Machines", EditorStyles.boldLabel);

            StateMachine[] machines = StateMachine.DebugInstances
                .Where(machine => IsShown(machine.Owner as UnityEngine.Object, DescribeState(machine.CurrentState)))
                .OrderBy(DescribeOwner)
                .ToArray();

            if (machines.Length == 0)
                EditorGUILayout.LabelField("None found.", EditorStyles.miniLabel);

            foreach (StateMachine machine in machines)
                DrawStateMachine(machine);
        }

        private void DrawStateMachine(StateMachine machine)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawOwnerHeader(machine.Owner as UnityEngine.Object, "StateMachine");

            DrawColouredLabel($"State: {DescribeState(machine.CurrentState)}, for {machine.DebugTimeInState:F1} s", ActiveColour, EditorStyles.boldLabel);

            int recentChanges = machine.DebugChanges.Count(change => Time.unscaledTime - change.Time <= 1f);

            if (recentChanges >= FlickerChangesPerSecond)
                EditorGUILayout.HelpBox($"Flicker: {recentChanges} changes of state in the last second.", MessageType.Warning);

            DrawTransitions(machine);
            DrawHistory(machine);

            EditorGUILayout.EndVertical();
        }

        private static void DrawTransitions(StateMachine machine)
        {
            foreach (StateMachine.DebugTransition transition in machine.DebugCurrentTransitions())
            {
                string prefix = transition.IsAnyState ? "Any" : "From here";
                string target = DescribeState(transition.To);
                string condition = DelegateNames.Describe(transition.Condition);

                try
                {
                    bool isMet = transition.Condition();
                    DrawColouredLabel($"{prefix} to {target}: {isMet}   ({condition})", isMet ? ActiveColour : FalseColour);
                }
                catch (Exception exception)
                {
                    DrawColouredLabel($"{prefix} to {target}: {exception.GetType().Name}   ({condition})", WarningColour);
                }
            }
        }

        private void DrawHistory(StateMachine machine)
        {
            bool isExpanded = expandedHistories.Contains(machine);
            bool shouldExpand = EditorGUILayout.Foldout(isExpanded, "Recent changes", true);

            if (shouldExpand)
                expandedHistories.Add(machine);
            else
                expandedHistories.Remove(machine);

            if (!shouldExpand)
                return;

            IReadOnlyList<StateMachine.DebugChange> changes = machine.DebugChanges;

            for (int i = changes.Count - 1; i >= Mathf.Max(0, changes.Count - HistoryLength); i--)
                EditorGUILayout.LabelField($"{changes[i].Time:F2} s, frame {changes[i].Frame}: {changes[i].StateName}");
        }

        private static string DescribeState(IState state)
        {
            return state == null ? "(not started)" : state.GetType().Name;
        }

        private static string DescribeOwner(StateMachine machine)
        {
            return machine.Owner is UnityEngine.Object owner && owner != null ? owner.name : string.Empty;
        }

        #endregion State Machines

        #region Shared Drawing

        private bool IsShown(UnityEngine.Object owner, string extraText)
        {
            if (selectionOnly && !IsSelected(owner))
                return false;

            if (string.IsNullOrEmpty(filter))
                return true;

            string ownerName = owner != null ? owner.name : string.Empty;

            return ownerName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || extraText.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSelected(UnityEngine.Object owner)
        {
            return owner is Component component && Selection.activeGameObject == component.gameObject;
        }

        private static void DrawOwnerHeader(UnityEngine.Object owner, string typeName)
        {
            EditorGUILayout.BeginHorizontal();

            string ownerName = owner != null ? owner.name : "(no owner set)";
            EditorGUILayout.LabelField($"{ownerName}  -  {typeName}", EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(owner == null))
            {
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(52f)))
                {
                    Selection.activeObject = owner;
                    EditorGUIUtility.PingObject(owner);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawColouredLabel(string text, Color colour, GUIStyle style = null)
        {
            Color previous = GUI.contentColor;
            GUI.contentColor = colour;
            EditorGUILayout.LabelField(text, style ?? EditorStyles.label);
            GUI.contentColor = previous;
        }

        #endregion Shared Drawing
    }
}
