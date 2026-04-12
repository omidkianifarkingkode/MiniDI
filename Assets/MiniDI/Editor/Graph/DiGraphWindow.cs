using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniDI.Editor.Diagnostics
{
    public class DiGraphWindow : EditorWindow
    {
        private DiGraphView _graphView;
        private PopupField<ServiceContainer> _containerSelector;
        private List<ServiceContainer> _availableContainers = new();

        [MenuItem("Window/MiniDI/Dependency Graph")]
        public static void ShowWindow()
        {
            var window = GetWindow<DiGraphWindow>("Mini-DI Graph");
            window.titleContent = new GUIContent("DI Graph");
            window.Show();
        }

        private void OnEnable()
        {
            ConstructGraphView();
            ConstructToolbar();

            // Try to load graph immediately if entering play mode with window open
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RefreshContainerList();
            }
        }

        private void ConstructGraphView()
        {
            _graphView = new DiGraphView
            {
                name = "DI Graph"
            };

            // CRITICAL: Tells the GraphView to fill the available window space
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private void ConstructToolbar()
        {
            var toolbar = new Toolbar();

            // 1. Refresh Button
            var refreshButton = new ToolbarButton(RefreshContainerList) { text = "Refresh Graph" };
            toolbar.Add(refreshButton);

            // 2. Container Selector Dropdown
            _containerSelector = new PopupField<ServiceContainer>(
                "Target Container:",
                _availableContainers,
                0,
                FormatContainerName,
                FormatContainerName
            );

            // 3. Listen for dropdown changes to update the graph
            _containerSelector.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != null)
                {
                    UpdateGraph(evt.newValue);
                }
            });

            toolbar.Add(_containerSelector);
            rootVisualElement.Add(toolbar);
        }

        private string FormatContainerName(ServiceContainer container)
        {
            return container != null && !string.IsNullOrEmpty(container.Name)
                ? container.Name
                : "Unknown Container";
        }

        private void RefreshContainerList()
        {
            // Fetch all active containers (Global + Scopes)
            _availableContainers = ServiceContainerEditorExtensions.GetActiveContainers();

            if (_availableContainers != null && _availableContainers.Count > 0)
            {
                _containerSelector.choices = _availableContainers;

                // If the current selection is no longer valid, reset to the first one
                if (!_availableContainers.Contains(_containerSelector.value))
                {
                    _containerSelector.value = _availableContainers[0];
                }

                UpdateGraph(_containerSelector.value);
            }
            else
            {
                // Clear dropdown and graph if no containers exist
                _containerSelector.choices = new List<ServiceContainer>();
                _containerSelector.value = null;
                _graphView.PopulateGraph(new List<ServiceDiagnosticInfo>()); // Pass empty list to clear

                Debug.LogWarning("Mini-DI: No active container found. Make sure you are in Play Mode and a container is Built.");
            }
        }

        private void UpdateGraph(ServiceContainer container)
        {
            if (container != null)
            {
                var diagnostics = container.GetDiagnostics();
                _graphView.PopulateGraph(diagnostics);
            }
        }
    }
}
