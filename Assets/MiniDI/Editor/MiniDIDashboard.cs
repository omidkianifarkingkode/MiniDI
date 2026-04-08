using UnityEditor;
using UnityEngine;
using MiniDI.Diagnostics;
using System.Collections.Generic;

namespace MiniDI.Editor
{
    public class MiniDIDashboard : EditorWindow
    {
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private ServiceContainer _selectedContainer;

        [MenuItem("Window/MiniDI/Dashboard")]
        public static void ShowWindow()
        {
            var window = GetWindow<MiniDIDashboard>("MiniDI Dashboard");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnEnable()
        {
            DiDiagnosticsRegistry.OnRegistryChanged += Repaint;
        }

        private void OnDisable()
        {
            DiDiagnosticsRegistry.OnRegistryChanged -= Repaint;
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying) Repaint(); // Keep states fresh
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view active MiniDI Containers.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // LEFT PANE: Container Tree
            DrawLeftPane();

            // Divider
            GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));

            // RIGHT PANE: Services List
            DrawRightPane();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPane()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));
            GUILayout.Label("Active Containers", EditorStyles.boldLabel);

            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, "box");

            if (DiDiagnosticsRegistry.RootContainers.Count == 0)
            {
                GUILayout.Label("No containers found.", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var root in DiDiagnosticsRegistry.RootContainers)
                {
                    DrawContainerNode(root, 0);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawContainerNode(DiDiagnosticsRegistry.TrackedContainer node, int indentLevel)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indentLevel * 15); // Indent for hierarchy

            GUIStyle style = _selectedContainer == node.Container ? EditorStyles.selectionRect : EditorStyles.label;

            if (GUILayout.Button(node.Name, style))
            {
                _selectedContainer = node.Container;
                GUI.FocusControl(null); // Remove focus to update selection color immediately
            }
            EditorGUILayout.EndHorizontal();

            // Draw children recursively
            foreach (var child in node.Children)
            {
                DrawContainerNode(child, indentLevel + 1);
            }
        }

        private void DrawRightPane()
        {
            EditorGUILayout.BeginVertical();

            if (_selectedContainer == null)
            {
                GUILayout.Label("Select a container from the left to view services.", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            GUILayout.Label("Registered Services", EditorStyles.boldLabel);

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Service Type", EditorStyles.toolbarButton, GUILayout.Width(180));
            GUILayout.Label("Implementation", EditorStyles.toolbarButton, GUILayout.Width(180));
            GUILayout.Label("Lifetime", EditorStyles.toolbarButton, GUILayout.Width(80));
            GUILayout.Label("State", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            bool alternateColor = false;
            foreach (var service in _selectedContainer.GetDiagnostics())
            {
                DrawServiceRow(service, alternateColor);
                alternateColor = !alternateColor;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawServiceRow(ServiceDiagnosticInfo info, bool alternate)
        {
            var bgColor = alternate ? new Color(0.3f, 0.3f, 0.3f, 0.2f) : new Color(0f, 0f, 0f, 0f);
            EditorGUI.DrawRect(EditorGUILayout.BeginHorizontal(), bgColor);

            GUILayout.Label(info.ServiceType?.Name ?? "Unknown", GUILayout.Width(180));
            GUILayout.Label(info.ImplementationType?.Name ?? "Delegate", GUILayout.Width(180));
            GUILayout.Label(info.Lifetime.ToString(), GUILayout.Width(80));

            string stateText = info.Lifetime == ServiceLifetime.Transient ? "N/A" : (info.IsInstantiated ? "Created" : "Pending");
            Color stateColor = info.IsInstantiated ? Color.green : Color.yellow;
            if (info.Lifetime == ServiceLifetime.Transient) stateColor = Color.gray;

            var prevColor = GUI.contentColor;
            GUI.contentColor = stateColor;
            GUILayout.Label(stateText, GUILayout.ExpandWidth(true));
            GUI.contentColor = prevColor;

            EditorGUILayout.EndHorizontal();
        }
    }
}
