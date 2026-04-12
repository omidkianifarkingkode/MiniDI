using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MiniDI.Editor.Diagnostics
{
    public class DiGraphView : GraphView
    {
        private readonly Dictionary<Type, DiNode> _nodeLookup = new();

        public DiGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            grid.StretchToParentSize();
            Insert(0, grid);
        }

        public void PopulateGraph(IEnumerable<ServiceDiagnosticInfo> diagnostics)
        {
            DeleteElements(graphElements);
            _nodeLookup.Clear();

            var diagList = diagnostics.ToList();
            if (diagList.Count == 0) return;

            // 1. Create Internal Nodes
            foreach (var info in diagList)
            {
                // Internal node: isExternal = false
                var node = new DiNode(info, false);
                _nodeLookup[info.ServiceType] = node;
                AddElement(node);
            }

            // 2. Discover External Dependencies (like IAudioService from Global)
            var allKnownTypes = diagList.Select(d => d.ServiceType).ToHashSet();
            var externalInfos = new List<ServiceDiagnosticInfo>();

            foreach (var info in diagList)
            {
                if (info.Dependencies == null) continue;

                foreach (var depType in info.Dependencies)
                {
                    if (!allKnownTypes.Contains(depType))
                    {
                        // This is a parent/external dependency!
                        var externalInfo = new ServiceDiagnosticInfo
                        {
                            ServiceType = depType,
                            Dependencies = Array.Empty<Type>() // We don't trace external sub-dependencies
                        };

                        // Add it to our sets so we only create one node per external type
                        if (allKnownTypes.Add(depType))
                        {
                            // Create Node with isExternal = true
                            var externalNode = new DiNode(externalInfo, true);
                            _nodeLookup[depType] = externalNode;
                            externalInfos.Add(externalInfo);
                            AddElement(externalNode);
                        }
                    }
                }
            }

            // Combine lists for the layout engine
            var layoutList = diagList.Concat(externalInfos).ToList();

            // 3. Create Edges
            foreach (var info in layoutList)
            {
                if (info.Dependencies == null) continue;

                if (_nodeLookup.TryGetValue(info.ServiceType, out var dependentNode))
                {
                    foreach (var depType in info.Dependencies)
                    {
                        if (_nodeLookup.TryGetValue(depType, out var providerNode))
                        {
                            var edge = providerNode.OutputPort.ConnectTo(dependentNode.InputPort);
                            AddElement(edge);
                        }
                    }
                }
            }

            // 4. Run Smart Layout
            ApplyDirectedGraphLayout(layoutList);
        }


        private void ApplyDirectedGraphLayout(List<ServiceDiagnosticInfo> diagnostics)
        {
            var infoLookup = diagnostics.ToDictionary(d => d.ServiceType);
            var depths = new Dictionary<Type, int>();
            var processing = new HashSet<Type>();

            int GetDepth(Type type)
            {
                if (depths.TryGetValue(type, out int calculatedDepth)) return calculatedDepth;
                if (!infoLookup.TryGetValue(type, out var info)) return 0;

                if (!processing.Add(type)) return 0;

                int maxDepDepth = -1;
                if (info.Dependencies != null)
                {
                    foreach (var dep in info.Dependencies)
                    {
                        int d = GetDepth(dep);
                        if (d > maxDepDepth) maxDepDepth = d;
                    }
                }

                processing.Remove(type);
                int depth = maxDepDepth + 1;
                depths[type] = depth;
                return depth;
            }

            int maxLayer = 0;
            foreach (var type in infoLookup.Keys)
            {
                int d = GetDepth(type);
                if (d > maxLayer) maxLayer = d;
            }

            var layers = new Dictionary<int, List<DiNode>>();
            for (int i = 0; i <= maxLayer; i++) layers[i] = new List<DiNode>();

            foreach (var kvp in depths)
            {
                if (_nodeLookup.TryGetValue(kvp.Key, out var node))
                {
                    layers[kvp.Value].Add(node);
                }
            }

            const float horizontalSpacing = 350f;
            const float verticalSpacing = 160f;

            var nodePositions = new Dictionary<Type, Vector2>();
            int maxNodesInAnyLayer = layers.Values.Max(l => l.Count);
            float maxColumnHeight = maxNodesInAnyLayer * verticalSpacing;

            for (int layerIdx = 0; layerIdx <= maxLayer; layerIdx++)
            {
                var layerNodes = layers[layerIdx];

                if (layerIdx > 0)
                {
                    layerNodes.Sort((a, b) =>
                    {
                        float aY = GetAverageDependencyY(a.ServiceType, infoLookup, nodePositions);
                        float bY = GetAverageDependencyY(b.ServiceType, infoLookup, nodePositions);
                        return aY.CompareTo(bY);
                    });
                }

                float layerHeight = layerNodes.Count * verticalSpacing;
                float startY = (maxColumnHeight - layerHeight) / 2f + 50f;

                for (int i = 0; i < layerNodes.Count; i++)
                {
                    var node = layerNodes[i];
                    float x = layerIdx * horizontalSpacing + 50f;
                    float y = startY + (i * verticalSpacing);

                    node.SetPosition(new Rect(x, y, 200, 120));
                    nodePositions[node.ServiceType] = new Vector2(x, y);
                }
            }
        }

        private float GetAverageDependencyY(Type serviceType, Dictionary<Type, ServiceDiagnosticInfo> infoLookup, Dictionary<Type, Vector2> nodePositions)
        {
            if (!infoLookup.TryGetValue(serviceType, out var info) || info.Dependencies == null || info.Dependencies.Length == 0)
                return 0f;

            float sumY = 0f;
            int count = 0;
            foreach (var dep in info.Dependencies)
            {
                if (nodePositions.TryGetValue(dep, out var pos))
                {
                    sumY += pos.y;
                    count++;
                }
            }
            return count > 0 ? sumY / count : 0f;
        }
    }
}
