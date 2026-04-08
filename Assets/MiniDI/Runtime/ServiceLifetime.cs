namespace MiniDI
{
    public enum ServiceLifetime
    {
        Singleton, // One instance globally
        Scoped,    // One instance per Container/Scope
        Transient  // New instance created every time
    }
}



