using System;

namespace MiniDI
{
    public interface IDiagnosticDescriptor
    {
        Type ServiceType { get; }
        Type ImplementationType { get; }
        ServiceLifetime Lifetime { get; }
        bool IsInstantiated { get; }
        Type[] Dependencies { get; }
    }
}