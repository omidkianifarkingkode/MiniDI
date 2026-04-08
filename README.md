# MiniDI 💉

A blazing-fast, zero-allocation micro Dependency Injection (DI) container for Unity. 

MiniDI is designed to be completely frictionless, offering $$O(1)$$ resolution times, memory safety, and a clean, Microsoft-style Builder API without the massive overhead of traditional DI frameworks.

## ✨ Features
* **Blazing Fast:** Service resolution is $$O(1)$$ using generic static caching.
* **Zero Allocations:** No garbage collection overhead (0 Bytes) after the initial resolution.
* **Microsoft-Style Builder API:** Strict separation between Registration and Initialization phases.
* **Scope Management:** Supports `Singleton`, `Scoped`, and `Transient` lifetimes.
* **Auto-Factory:** Automatic constructor injection via reflection (cached as delegates for performance).
* **UPM Ready:** Install easily via Unity Package Manager.

## 📦 Installation

**Via Unity Package Manager (UPM)**
1. Open Unity and go to `Window` > `Package Manager`.
2. Click the `+` button in the top-left corner.
3. Select `Add package from git URL...`
4. Paste the repository URL: `"com.kingkode.mini-di": "https://github.com/omidkianifarkingkode/MiniDI.git?path=Assets/MiniDI"


This ensures UPM fetches exactly the package files without trying to import your `ProjectSettings` or extra example scenes into the user's project.`

## 🚀 Quick Start

Use the Builder API to register and initialize your services securely.
```csharp
using MiniDI;
using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
private ServiceContainer _container;

private void Awake()
{
_container = ServiceContainer.CreateBuilder(Debug.unityLogger)
// 1. REGISTRATION PHASE
.ConfigureServices(services =>
{
services.Register<ILogger>(Debug.unityLogger);
services.Register<IPlayerService, PlayerService>(ServiceLifetime.Singleton);
})
// 2. INITIALIZATION PHASE
.OnInitialize(resolver =>
{
if (resolver.TryResolve<IPlayerService>(out var playerService))
{
playerService.Initialize();
}
})
// 3. BUILD
.Build();
}

private void OnDestroy()
{
_container?.Dispose();
}
}```
