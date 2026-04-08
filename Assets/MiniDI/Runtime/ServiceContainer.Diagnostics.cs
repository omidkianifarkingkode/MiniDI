#if UNITY_EDITOR
using System.Collections.Generic;
using MiniDI.Diagnostics;

namespace MiniDI
{
    public partial class ServiceContainer
    {
        internal void RegisterWithDiagnostics(string name)
        {
            DiDiagnosticsRegistry.Register(this, name, _parent);
        }

        public IEnumerable<ServiceDiagnosticInfo> GetDiagnostics()
        {
            var diagnostics = new List<ServiceDiagnosticInfo>();

            for (int i = 0; i < _services.Length; i++)
            {
                if (_services[i] is IDiagnosticDescriptor diag)
                {
                    diagnostics.Add(new ServiceDiagnosticInfo
                    {
                        ServiceType = diag.ServiceType,
                        ImplementationType = diag.ImplementationType,
                        Lifetime = diag.Lifetime,
                        IsInstantiated = diag.IsInstantiated
                    });
                }
            }

            return diagnostics;
        }

        private void CleanupDiagnostics()
        {
            DiDiagnosticsRegistry.Unregister(this);
        }
    }
}
#endif

