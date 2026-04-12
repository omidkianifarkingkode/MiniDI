using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using MiniDI;

namespace MiniDI.Editor.Diagnostics
{
    public class DiNode : Node
    {
        public Type ServiceType { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        public DiNode(ServiceDiagnosticInfo info, bool isExternal)
        {
            ServiceType = info.ServiceType;
            title = info.ServiceType.GetNiceTypeName();

            // If it's external, we don't necessarily know the lifetime (it belongs to another container)
            string lifetimeText = isExternal ? "External (Parent)" : $"Lifetime: {info.Lifetime}";
            var lifetimeLabel = new Label(lifetimeText);
            mainContainer.Add(lifetimeLabel);

            if (isExternal)
            {
                // Visual style for nodes not in this container
                style.opacity = 0.8f;
                // Give it a distinct "Ghost" look (darker header)
                titleContainer.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f, 0.9f));
            }

            CreatePorts();
        }

        private void CreatePorts()
        {
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Depends On";
            inputContainer.Add(InputPort);

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = "Injected Into";
            outputContainer.Add(OutputPort);

            RefreshPorts();
            RefreshExpandedState();
        }
    }
}
