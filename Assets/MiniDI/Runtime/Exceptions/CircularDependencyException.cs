// CircularDependencyException.cs (Runtime Package)
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniDI
{
    public class CircularDependencyException : Exception
    {
        private readonly List<Type> _resolutionPath = new List<Type>();
        private readonly Type _targetType;

        internal CircularDependencyException(Type targetType)
        {
            _targetType = targetType;
            _resolutionPath.Add(targetType);
        }

        internal void AddToPath(Type type)
        {
            // Only add to the path if we haven't completed the circle visually
            if (_resolutionPath.Count > 0 && _resolutionPath[_resolutionPath.Count - 1] != _targetType)
            {
                _resolutionPath.Add(type);
            }
        }

        public override string Message
        {
            get
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Circular dependency detected while resolving '{_targetType.Name}'!");
                sb.Append("Resolution Path: ");

                // Read backwards because we built the list as the exception bubbled UP the stack
                for (int i = _resolutionPath.Count - 1; i >= 0; i--)
                {
                    sb.Append(_resolutionPath[i].Name);
                    if (i > 0) sb.Append(" -> ");
                }

                // Cap off the circle
                sb.Append($" -> {_targetType.Name}");

                return sb.ToString();
            }
        }
    }
}
