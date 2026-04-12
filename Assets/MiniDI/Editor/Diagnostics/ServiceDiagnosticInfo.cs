using System;
using MiniDI;

namespace MiniDI.Editor.Diagnostics
{
    public class ServiceDiagnosticInfo
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public bool IsInstantiated { get; set; }
        public Type[] Dependencies { get; set; } = Array.Empty<Type>();
    }
}
