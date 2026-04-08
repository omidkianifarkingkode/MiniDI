#if UNITY_EDITOR

using System;

namespace MiniDI.Diagnostics
{
    public struct ServiceDiagnosticInfo
    {
        public Type ServiceType;
        public Type ImplementationType;
        public ServiceLifetime Lifetime;
        public bool IsInstantiated;
    }
}
#endif